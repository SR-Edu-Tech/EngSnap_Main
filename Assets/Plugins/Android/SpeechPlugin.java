package com.yourgame.speech;

import android.app.Activity;
import android.content.Context;
import android.content.Intent;
import android.media.AudioManager;
import android.os.Bundle;
import android.os.Handler;
import android.speech.RecognitionListener;
import android.speech.RecognizerIntent;
import android.speech.SpeechRecognizer;
import com.unity3d.player.UnityPlayer;

import java.util.ArrayList;
import java.util.Locale;

public class SpeechPlugin implements RecognitionListener {

    private static SpeechPlugin instance;
    private SpeechRecognizer speechRecognizer;
    private Activity activity;
    private String unityGameObjectName;
    private String unityCallbackMethod;

    private AudioManager audioManager;
    private int originalMusicVolume  = -1;
    private int originalSystemVolume = -1;
    private Handler unMuteHandler = new Handler();

    // ── Singleton ──────────────────────────────────────────────────────────────

    public static SpeechPlugin getInstance() {
        if (instance == null) {
            instance = new SpeechPlugin();
        }
        return instance;
    }

    // ── Availability check (called from Unity before init) ─────────────────────

    public static boolean isRecognitionAvailable(Context context) {
        return SpeechRecognizer.isRecognitionAvailable(context);
    }

    // ── Init ───────────────────────────────────────────────────────────────────

    public void init(final String gameObjectName, final String callbackMethod) {
        this.unityGameObjectName = gameObjectName;
        this.unityCallbackMethod = callbackMethod;
        this.activity = UnityPlayer.currentActivity;

        if (this.activity != null) {
            this.audioManager = (AudioManager) activity.getSystemService(Context.AUDIO_SERVICE);
        }

        activity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                if (speechRecognizer != null) {
                    speechRecognizer.destroy();
                }
                speechRecognizer = SpeechRecognizer.createSpeechRecognizer(activity);
                speechRecognizer.setRecognitionListener(SpeechPlugin.this);
            }
        });
    }

    // ── Volume muting ──────────────────────────────────────────────────────────
    // Wrapped in try/catch because Lenovo and other tablets running Android 12+
    // throw a SecurityException from setStreamVolume() when Do Not Disturb mode
    // is active or ACCESS_NOTIFICATION_POLICY is not granted.
    // Beep muting is cosmetic — skip it silently rather than crash.

    private void muteBeep() {
        if (audioManager == null) return;
        unMuteHandler.removeCallbacksAndMessages(null);
        try {
            int musicVol = audioManager.getStreamVolume(AudioManager.STREAM_MUSIC);
            if (musicVol > 0) {
                originalMusicVolume = musicVol;
                audioManager.setStreamVolume(AudioManager.STREAM_MUSIC, 0, 0);
            }
        } catch (Exception e) {
            originalMusicVolume = -1; // DND restriction — skip
        }
        try {
            int sysVol = audioManager.getStreamVolume(AudioManager.STREAM_SYSTEM);
            if (sysVol > 0) {
                originalSystemVolume = sysVol;
                audioManager.setStreamVolume(AudioManager.STREAM_SYSTEM, 0, 0);
            }
        } catch (Exception e) {
            originalSystemVolume = -1; // DND restriction — skip
        }
    }

    private void unmuteBeep() {
        unMuteHandler.postDelayed(new Runnable() {
            @Override
            public void run() {
                if (audioManager == null) return;
                if (originalMusicVolume != -1) {
                    try {
                        audioManager.setStreamVolume(AudioManager.STREAM_MUSIC, originalMusicVolume, 0);
                    } catch (Exception e) { /* ignore */ }
                    originalMusicVolume = -1;
                }
                if (originalSystemVolume != -1) {
                    try {
                        audioManager.setStreamVolume(AudioManager.STREAM_SYSTEM, originalSystemVolume, 0);
                    } catch (Exception e) { /* ignore */ }
                    originalSystemVolume = -1;
                }
            }
        }, 500);
    }

    // ── Public API ─────────────────────────────────────────────────────────────

    public void startListening() {
        muteBeep();

        activity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                if (speechRecognizer == null) {
                    UnityPlayer.UnitySendMessage(unityGameObjectName, "OnSpeechError",
                            "SpeechRecognizer not initialized");
                    return;
                }

                Intent intent = new Intent(RecognizerIntent.ACTION_RECOGNIZE_SPEECH);
                intent.putExtra(RecognizerIntent.EXTRA_LANGUAGE_MODEL,
                        RecognizerIntent.LANGUAGE_MODEL_FREE_FORM);

                // Use device locale — fixes ERROR_LANGUAGE_NOT_SUPPORTED on tablets
                intent.putExtra(RecognizerIntent.EXTRA_LANGUAGE, Locale.getDefault());
                intent.putExtra(RecognizerIntent.EXTRA_LANGUAGE_PREFERENCE, "en-US");
                intent.putExtra(RecognizerIntent.EXTRA_ONLY_RETURN_LANGUAGE_PREFERENCE, false);

                intent.putExtra(RecognizerIntent.EXTRA_MAX_RESULTS, 1);
                intent.putExtra(RecognizerIntent.EXTRA_PARTIAL_RESULTS, true);

                // Prefer offline — important for Wi-Fi-only tablets
                intent.putExtra(RecognizerIntent.EXTRA_PREFER_OFFLINE, true);

                try {
                    speechRecognizer.startListening(intent);
                } catch (Exception e) {
                    UnityPlayer.UnitySendMessage(unityGameObjectName, "OnSpeechError",
                            "startListening exception: " + e.getMessage());
                }
            }
        });
    }

    public void stopListening() {
        muteBeep();

        activity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                if (speechRecognizer != null) {
                    speechRecognizer.stopListening();
                }
            }
        });
    }

    public void destroy() {
        activity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                if (speechRecognizer != null) {
                    speechRecognizer.destroy();
                    speechRecognizer = null;
                }
            }
        });
    }

    // ── RecognitionListener callbacks ──────────────────────────────────────────

    @Override
    public void onReadyForSpeech(Bundle params) {
        unmuteBeep();
        UnityPlayer.UnitySendMessage(unityGameObjectName, "OnSpeechReady", "");
    }

    @Override
    public void onBeginningOfSpeech() {
        UnityPlayer.UnitySendMessage(unityGameObjectName, "OnSpeechBegin", "");
    }

    @Override
    public void onEndOfSpeech() {
        muteBeep();
        UnityPlayer.UnitySendMessage(unityGameObjectName, "OnSpeechEnd", "");
    }

    @Override
    public void onResults(Bundle results) {
        unmuteBeep();
        ArrayList<String> matches = results.getStringArrayList(SpeechRecognizer.RESULTS_RECOGNITION);
        if (matches != null && !matches.isEmpty()) {
            UnityPlayer.UnitySendMessage(unityGameObjectName, unityCallbackMethod, matches.get(0));
        }
    }

    @Override
    public void onPartialResults(Bundle partialResults) {
        ArrayList<String> partial = partialResults.getStringArrayList(SpeechRecognizer.RESULTS_RECOGNITION);
        if (partial != null && !partial.isEmpty()) {
            UnityPlayer.UnitySendMessage(unityGameObjectName, "OnSpeechPartial", partial.get(0));
        }
    }

    @Override
    public void onError(int error) {
        unmuteBeep();
        String msg;
        switch (error) {
            case SpeechRecognizer.ERROR_NO_MATCH:                 msg = "No speech match found";           break;
            case SpeechRecognizer.ERROR_SPEECH_TIMEOUT:           msg = "Speech input timed out";          break;
            case SpeechRecognizer.ERROR_AUDIO:                    msg = "Audio recording error";           break;
            case SpeechRecognizer.ERROR_NETWORK:                  msg = "Network error";                   break;
            case SpeechRecognizer.ERROR_INSUFFICIENT_PERMISSIONS: msg = "Missing RECORD_AUDIO permission"; break;
            default:                                              msg = "Error code: " + error;            break;
        }
        UnityPlayer.UnitySendMessage(unityGameObjectName, "OnSpeechError", msg);
    }

    @Override public void onRmsChanged(float rmsdB)            { }
    @Override public void onBufferReceived(byte[] buffer)      { }
    @Override public void onEvent(int eventType, Bundle params) { }
}