package com.yourgame.speech;

import android.app.Activity;
import android.content.Intent;
import android.os.Bundle;
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

    // ── Singleton ──────────────────────────────────────────────────────────────
    public static SpeechPlugin getInstance() {
        if (instance == null) {
            instance = new SpeechPlugin();
        }
        return instance;
    }

    // ── Init ────────────────────────────────────────────────────────────────────
    // Call this from Unity C# once at Start()
    // gameObjectName  : name of the C# GameObject that receives callbacks
    // callbackMethod  : name of the C# method to call with the result string
    public void init(final String gameObjectName, final String callbackMethod) {
        this.unityGameObjectName = gameObjectName;
        this.unityCallbackMethod = callbackMethod;
        this.activity = UnityPlayer.currentActivity;

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

    // ── Start Listening ─────────────────────────────────────────────────────────
    public void startListening() {
        activity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                Intent intent = new Intent(RecognizerIntent.ACTION_RECOGNIZE_SPEECH);
                intent.putExtra(RecognizerIntent.EXTRA_LANGUAGE_MODEL,
                        RecognizerIntent.LANGUAGE_MODEL_FREE_FORM);
                intent.putExtra(RecognizerIntent.EXTRA_LANGUAGE, Locale.ENGLISH);
                intent.putExtra(RecognizerIntent.EXTRA_MAX_RESULTS, 1);
                // Partial results while speaking (optional)
                intent.putExtra(RecognizerIntent.EXTRA_PARTIAL_RESULTS, true);
                speechRecognizer.startListening(intent);
            }
        });
    }

    // ── Stop Listening ──────────────────────────────────────────────────────────
    public void stopListening() {
        activity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                if (speechRecognizer != null) {
                    speechRecognizer.stopListening();
                }
            }
        });
    }

    // ── Destroy ─────────────────────────────────────────────────────────────────
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

    // ── RecognitionListener callbacks ───────────────────────────────────────────

    @Override
    public void onReadyForSpeech(Bundle params) {
        // Microphone opened, listening started
        UnityPlayer.UnitySendMessage(unityGameObjectName, "OnSpeechReady", "");
    }

    @Override
    public void onBeginningOfSpeech() {
        // User started speaking
        UnityPlayer.UnitySendMessage(unityGameObjectName, "OnSpeechBegin", "");
    }

    @Override
    public void onEndOfSpeech() {
        // User stopped speaking — processing begins
        UnityPlayer.UnitySendMessage(unityGameObjectName, "OnSpeechEnd", "");
    }

    @Override
    public void onResults(Bundle results) {
        // Final transcription result
        ArrayList<String> matches =
                results.getStringArrayList(SpeechRecognizer.RESULTS_RECOGNITION);
        if (matches != null && !matches.isEmpty()) {
            UnityPlayer.UnitySendMessage(unityGameObjectName, unityCallbackMethod, matches.get(0));
        }
    }

    @Override
    public void onPartialResults(Bundle partialResults) {
        // Real-time partial result while user is still speaking
        ArrayList<String> partial =
                partialResults.getStringArrayList(SpeechRecognizer.RESULTS_RECOGNITION);
        if (partial != null && !partial.isEmpty()) {
            UnityPlayer.UnitySendMessage(unityGameObjectName, "OnSpeechPartial", partial.get(0));
        }
    }

    @Override
    public void onError(int error) {
        String msg;
        switch (error) {
            case SpeechRecognizer.ERROR_NO_MATCH:       msg = "No speech match found";      break;
            case SpeechRecognizer.ERROR_SPEECH_TIMEOUT: msg = "Speech input timed out";     break;
            case SpeechRecognizer.ERROR_AUDIO:          msg = "Audio recording error";      break;
            case SpeechRecognizer.ERROR_NETWORK:        msg = "Network error";              break;
            case SpeechRecognizer.ERROR_INSUFFICIENT_PERMISSIONS:
                                                        msg = "Missing RECORD_AUDIO permission"; break;
            default:                                    msg = "Error code: " + error;       break;
        }
        UnityPlayer.UnitySendMessage(unityGameObjectName, "OnSpeechError", msg);
    }

    @Override public void onRmsChanged(float rmsdB) { /* mic volume — optional */ }
    @Override public void onBufferReceived(byte[] buffer) { /* raw audio — optional */ }
    @Override public void onEvent(int eventType, Bundle params) { /* reserved — optional */ }
}
