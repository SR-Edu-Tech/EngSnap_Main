#import "UnityInterface.h"
#import <Foundation/Foundation.h>
#import <Speech/Speech.h>
#import <AVFoundation/AVFoundation.h>

// ── State ───────────────────────────────────────────────────────────────────
static NSString*                              s_gameObject  = @"SpeechManager";
static SFSpeechRecognizer*                    s_recognizer  = nil;
static AVAudioEngine*                         s_audioEngine = nil;
static SFSpeechAudioBufferRecognitionRequest* s_request     = nil;
static SFSpeechRecognitionTask*               s_task        = nil;

// ── Helpers ─────────────────────────────────────────────────────────────────
static void SendToUnity(const char* method, NSString* message) {
    if (s_gameObject != nil) {
        UnitySendMessage([s_gameObject UTF8String], method, [message UTF8String]);
    }
}

static void Cleanup() {
    dispatch_async(dispatch_get_main_queue(), ^{
        if (s_audioEngine != nil) {
            if (s_audioEngine.isRunning) {
                [s_audioEngine stop];
            }
            [s_audioEngine.inputNode removeTapOnBus:0];
        }
       
        s_request = nil;
        s_task    = nil;
       
        // Restore AVAudioSession category to Ambient so that Unity's main audio system (FMOD) recovers
        NSError* error = nil;
        AVAudioSession* session = [AVAudioSession sharedInstance];
        [session setCategory:AVAudioSessionCategoryAmbient error:&error];
        [session setMode:AVAudioSessionModeDefault error:&error];
       
        // Force Unity's internal audio manager to reactivate and regain device control
        UnitySetAudioSessionActive(true);
    });
}

// ── Public C functions (called from C# via DllImport) ──────────────────────
#ifdef __cplusplus
extern "C" {
#endif

void STT_Init(const char* gameObjectName) {
    s_gameObject = [NSString stringWithUTF8String:gameObjectName];
    if (s_recognizer == nil) {
        s_recognizer = [[SFSpeechRecognizer alloc] initWithLocale:[NSLocale localeWithLocaleIdentifier:@"en-US"]];
    }
    if (s_audioEngine == nil) {
        s_audioEngine = [[AVAudioEngine alloc] init];
    }
}

void STT_RequestPermission() {
    AVAudioSession* session = [AVAudioSession sharedInstance];
    if ([session respondsToSelector:@selector(requestRecordPermission:)]) {
        [session requestRecordPermission:^(BOOL granted) {
            if (!granted) {
                dispatch_async(dispatch_get_main_queue(), ^{
                    SendToUnity("OnSpeechError", @"Microphone permission denied");
                });
                return;
            }
           
            [SFSpeechRecognizer requestAuthorization:^(SFSpeechRecognizerAuthorizationStatus status) {
                dispatch_async(dispatch_get_main_queue(), ^{
                    if (status == SFSpeechRecognizerAuthorizationStatusAuthorized) {
                        SendToUnity("OnPermissionGranted", @"");
                    } else {
                        SendToUnity("OnSpeechError", @"Speech recognition permission denied");
                    }
                });
            }];
        }];
    } else {
        dispatch_async(dispatch_get_main_queue(), ^{
            SendToUnity("OnSpeechError", @"Microphone permission API not supported on this iOS version");
        });
    }
}

void STT_StartListening() {
    if (s_task != nil) {
        [s_task cancel];
        s_task = nil;
    }
   
    if (s_recognizer == nil) {
        s_recognizer = [[SFSpeechRecognizer alloc] initWithLocale:[NSLocale localeWithLocaleIdentifier:@"en-US"]];
    }
    if (s_audioEngine == nil) {
        s_audioEngine = [[AVAudioEngine alloc] init];
    }
   
    if (s_recognizer == nil) {
        SendToUnity("OnSpeechError", @"Speech recognizer not supported on this device.");
        return;
    }
   
    if (!s_recognizer.isAvailable) {
        SendToUnity("OnSpeechError", @"Speech recognition is currently unavailable.");
        return;
    }
   
    NSError* error = nil;
    AVAudioSession* session = [AVAudioSession sharedInstance];
   
    [session setCategory:AVAudioSessionCategoryPlayAndRecord
             withOptions:AVAudioSessionCategoryOptionDefaultToSpeaker | AVAudioSessionCategoryOptionAllowBluetooth | AVAudioSessionCategoryOptionMixWithOthers
                   error:&error];
    if (error) {
        SendToUnity("OnSpeechError", [NSString stringWithFormat:@"Failed to set audio session category: %@", [error localizedDescription]]);
        return;
    }
   
    [session setMode:AVAudioSessionModeMeasurement error:&error];
    if (error) {
        SendToUnity("OnSpeechError", [NSString stringWithFormat:@"Failed to set audio session mode: %@", [error localizedDescription]]);
        return;
    }
   
    [session setActive:YES withOptions:AVAudioSessionSetActiveOptionNotifyOthersOnDeactivation error:&error];
    if (error) {
        SendToUnity("OnSpeechError", [NSString stringWithFormat:@"Failed to activate audio session: %@", [error localizedDescription]]);
        return;
    }
   
    s_request = [[SFSpeechAudioBufferRecognitionRequest alloc] init];
    if (s_request == nil) {
        SendToUnity("OnSpeechError", @"Failed to create recognition request.");
        Cleanup();
        return;
    }
   
    s_request.shouldReportPartialResults = YES;
   
    AVAudioInputNode* inputNode = s_audioEngine.inputNode;
    if (inputNode == nil) {
        SendToUnity("OnSpeechError", @"Audio Engine input node is unavailable.");
        Cleanup();
        return;
    }
   
    [inputNode removeTapOnBus:0];
   
    __block BOOL firstResult = YES;
   
    s_task = [s_recognizer recognitionTaskWithRequest:s_request
                                       resultHandler:^(SFSpeechRecognitionResult* result, NSError* err) {
        if (result) {
            NSString* text = result.bestTranscription.formattedString;
           
            if (result.isFinal) {
                SendToUnity("OnSpeechResult", text);
                Cleanup();
                SendToUnity("OnSpeechEnd", @"");
            } else {
                if (firstResult) {
                    firstResult = NO;
                    SendToUnity("OnSpeechBegin", @"");
                }
                SendToUnity("OnSpeechPartial", text);
            }
        }
       
        if (err != nil) {
            if (err.code != 3010) {
                SendToUnity("OnSpeechError", [err localizedDescription]);
            }
            Cleanup();
        }
    }];
   
    AVAudioFormat* fmt = [inputNode outputFormatForBus:0];
    if (fmt.sampleRate == 0) {
        SendToUnity("OnSpeechError", @"Microphone sample rate is 0. Check microphone hardware accessibility.");
        Cleanup();
        return;
    }
   
    [inputNode installTapOnBus:0 bufferSize:1024 format:fmt
                         block:^(AVAudioPCMBuffer* buf, AVAudioTime* when) {
        if (s_request != nil) {
            [s_request appendAudioPCMBuffer:buf];
        }
    }];
   
    [s_audioEngine prepare];
    [s_audioEngine startAndReturnError:&error];
    if (error) {
        SendToUnity("OnSpeechError", [NSString stringWithFormat:@"Failed to start audio engine: %@", [error localizedDescription]]);
        Cleanup();
        return;
    }
   
    SendToUnity("OnSpeechReady", @"");
}

void STT_StopListening() {
    dispatch_async(dispatch_get_main_queue(), ^{
        if (s_audioEngine.isRunning) {
            [s_audioEngine stop];
        }
        if (s_request != nil) {
            [s_request endAudio];
        }
    });
}

void STT_Destroy() {
    dispatch_async(dispatch_get_main_queue(), ^{
        if (s_task != nil) {
            [s_task cancel];
            s_task = nil;
        }
        Cleanup();
    });
}

#ifdef __cplusplus
}
#endif