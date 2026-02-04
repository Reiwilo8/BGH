package com.project.core.speech;

import android.app.Activity;
import android.content.Intent;
import android.os.Build;
import android.os.Handler;
import android.os.Looper;
import android.speech.tts.TextToSpeech;
import android.speech.tts.UtteranceProgressListener;

import com.unity3d.player.UnityPlayer;

import java.util.Locale;
import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.ScheduledThreadPoolExecutor;
import java.util.concurrent.TimeUnit;
import java.util.concurrent.atomic.AtomicInteger;

public final class TtsBridgeAndroid {
    private final Activity activity;
    private final Handler mainHandler;

    private TextToSpeech tts;
    private volatile boolean ready;

    private volatile String unityGameObject;
    private volatile String unityCallbackMethod;

    private volatile String requestedLangTag = "en";
    private volatile String activeLangTag = "";
    private volatile String lastWorkingLangTag = "";

    private final AtomicInteger uttCounter = new AtomicInteger(0);
    private final AtomicInteger activeCount = new AtomicInteger(0);

    private final ConcurrentHashMap<String, Long> deadlineByUtt = new ConcurrentHashMap<>();

    private final ScheduledThreadPoolExecutor watchdogExec;
    private volatile boolean watchdogStarted;

    public TtsBridgeAndroid(Activity activity, String unityGameObject, String unityCallbackMethod) {
        this.activity = activity;
        this.mainHandler = new Handler(Looper.getMainLooper());
        this.unityGameObject = unityGameObject;
        this.unityCallbackMethod = unityCallbackMethod;

        this.watchdogExec = new ScheduledThreadPoolExecutor(1);
        this.watchdogExec.setRemoveOnCancelPolicy(true);

        this.lastWorkingLangTag = localeToTagCompat(Locale.getDefault());

        initTts();
    }

    public static TtsBridgeAndroid create(String unityGameObject, String unityCallbackMethod) {
        Activity a = UnityPlayer.currentActivity;
        return new TtsBridgeAndroid(a, unityGameObject, unityCallbackMethod);
    }

    public boolean isReady() {
        return ready;
    }

    public boolean isSpeaking() {
        return activeCount.get() > 0;
    }

    public void setUnityCallback(String unityGameObject, String unityCallbackMethod) {
        this.unityGameObject = unityGameObject;
        this.unityCallbackMethod = unityCallbackMethod;
    }

    public String getActiveLanguageTag() {
        return activeLangTag;
    }

    public String getLastWorkingLanguageTag() {
        return lastWorkingLangTag;
    }

    public int checkLanguageAvailability(String languageTag) {
        if (tts == null || !ready) return TextToSpeech.LANG_NOT_SUPPORTED;

        String tag = normalizeTag(languageTag);
        final Locale locale = toLocaleCompat(tag);

        final int[] out = new int[] { TextToSpeech.LANG_NOT_SUPPORTED };
        try {
            if (Build.VERSION.SDK_INT >= 21) {
                out[0] = tts.isLanguageAvailable(locale);
            } else {
                out[0] = tts.isLanguageAvailable(locale);
            }
        } catch (Throwable ignored) {
            out[0] = TextToSpeech.LANG_NOT_SUPPORTED;
        }
        return out[0];
    }

    public boolean requestInstallTtsData() {
        try {
            Intent i = new Intent();
            i.setAction(TextToSpeech.Engine.ACTION_INSTALL_TTS_DATA);
            i.addFlags(Intent.FLAG_ACTIVITY_NEW_TASK);
            activity.startActivity(i);
            return true;
        } catch (Throwable ignored) {
            return false;
        }
    }

    public void setLanguage(String languageTag) {
        String tag = normalizeTag(languageTag);
        requestedLangTag = tag;

        if (!ready || tts == null) return;

        final Locale locale = toLocaleCompat(tag);

        mainHandler.post(() -> {
            try {
                if (tts == null) return;

                int res = tts.setLanguage(locale);

                if (isLangOk(res)) {
                    activeLangTag = tag;
                    lastWorkingLangTag = tag;
                    sendUnityEvent("lang_ok", tag);
                    return;
                }

                String fallbackTag = lastWorkingLangTag;
                if (fallbackTag == null || fallbackTag.trim().isEmpty())
                    fallbackTag = localeToTagCompat(Locale.getDefault());

                final String fb = fallbackTag;
                int res2 = tts.setLanguage(toLocaleCompat(fb));

                if (isLangOk(res2)) {
                    activeLangTag = fb;
                    sendUnityEvent("lang_fallback", tag + ">" + fb + "|" + res);
                } else {
                    activeLangTag = "";
                    sendUnityEvent("lang_error", tag + "|" + res);
                }
            } catch (Throwable ignored) {
                sendUnityEvent("lang_error", tag + "|exception");
            }
        });
    }

    public String speak(String text, boolean flush) {
        if (text == null) return null;
        String t = text.trim();
        if (t.isEmpty()) return null;

        if (tts == null || !ready) return null;

        final String uttId = "utt_" + uttCounter.incrementAndGet();
        final long deadline = computeDeadlineMs(t);

        deadlineByUtt.put(uttId, System.currentTimeMillis() + deadline);

        int before = activeCount.getAndIncrement();
        if (before == 0) sendUnityEvent("start_any", uttId);

        if (!watchdogStarted) startWatchdog();

        mainHandler.post(() -> {
            try {
                if (tts == null) {
                    forceDone(uttId, true);
                    return;
                }

                int queueMode = flush ? TextToSpeech.QUEUE_FLUSH : TextToSpeech.QUEUE_ADD;

                String want = requestedLangTag;
                if (want != null && !want.trim().isEmpty() && (activeLangTag == null || !want.equals(activeLangTag))) {
                    applyLanguageInline(want);
                }

                tts.speak(t, queueMode, null, uttId);
            } catch (Throwable ex) {
                forceDone(uttId, true);
            }
        });

        return uttId;
    }

    public void stopAll() {
        if (tts == null) return;

        deadlineByUtt.clear();
        activeCount.set(0);

        mainHandler.post(() -> {
            try {
                if (tts != null) tts.stop();
            } catch (Throwable ignored) {
            }
        });

        sendUnityEvent("stop", "");
    }

    public void shutdown() {
        try {
            watchdogExec.shutdownNow();
        } catch (Throwable ignored) {
        }

        mainHandler.post(() -> {
            try {
                if (tts != null) {
                    tts.stop();
                    tts.shutdown();
                }
            } catch (Throwable ignored) {
            } finally {
                tts = null;
                ready = false;
            }
        });
    }

    private void initTts() {
        mainHandler.post(() -> {
            try {
                tts = new TextToSpeech(activity, status -> {
                    ready = (status == TextToSpeech.SUCCESS);

                    if (ready && tts != null) {
                        try {
                            tts.setOnUtteranceProgressListener(new Listener());
                        } catch (Throwable ignored) {
                        }

                        try {
                            Locale cur = tts.getLanguage();
                            if (cur != null) {
                                String tag = localeToTagCompat(cur);
                                if (tag != null && !tag.isEmpty()) {
                                    activeLangTag = tag;
                                    lastWorkingLangTag = tag;
                                }
                            }
                        } catch (Throwable ignored) {
                        }

                        applyLanguageInline(requestedLangTag);

                        sendUnityEvent("ready", "");
                    } else {
                        sendUnityEvent("error_init", String.valueOf(status));
                    }
                });
            } catch (Throwable ex) {
                ready = false;
                tts = null;
                sendUnityEvent("error_init", "exception");
            }
        });
    }

    private void applyLanguageInline(String tag) {
        if (tts == null) return;

        String want = normalizeTag(tag);
        Locale loc = toLocaleCompat(want);

        try {
            int res = tts.setLanguage(loc);
            if (isLangOk(res)) {
                activeLangTag = want;
                lastWorkingLangTag = want;
                return;
            }

            String fallbackTag = lastWorkingLangTag;
            if (fallbackTag == null || fallbackTag.trim().isEmpty())
                fallbackTag = localeToTagCompat(Locale.getDefault());

            int res2 = tts.setLanguage(toLocaleCompat(fallbackTag));
            if (isLangOk(res2)) {
                activeLangTag = fallbackTag;
            } else {
                activeLangTag = "";
            }
        } catch (Throwable ignored) {
        }
    }

    private static boolean isLangOk(int res) {
        return res != TextToSpeech.LANG_MISSING_DATA && res != TextToSpeech.LANG_NOT_SUPPORTED;
    }

    private void startWatchdog() {
        watchdogStarted = true;
        try {
            watchdogExec.scheduleAtFixedRate(this::tickWatchdog, 250, 250, TimeUnit.MILLISECONDS);
        } catch (Throwable ignored) {
        }
    }

    private void tickWatchdog() {
        if (deadlineByUtt.isEmpty()) return;

        long now = System.currentTimeMillis();

        for (Map.Entry<String, Long> e : deadlineByUtt.entrySet()) {
            String uttId = e.getKey();
            Long dl = e.getValue();
            if (dl == null) continue;

            if (now >= dl) {
                forceDone(uttId, false);
            }
        }
    }

    private void forceDone(String uttId, boolean error) {
        Long removed = deadlineByUtt.remove(uttId);
        if (removed == null) return;

        int left = activeCount.decrementAndGet();
        if (left < 0) {
            activeCount.set(0);
            left = 0;
        }

        sendUnityEvent(error ? "error" : "done", uttId);

        if (left == 0) sendUnityEvent("done_any", uttId);
    }

    private void onListenerStart(String uttId) {
        sendUnityEvent("utt_start", uttId);
    }

    private void onListenerDone(String uttId) {
        Long removed = deadlineByUtt.remove(uttId);
        if (removed == null) {
            sendUnityEvent("done", uttId);
            return;
        }

        int left = activeCount.decrementAndGet();
        if (left < 0) {
            activeCount.set(0);
            left = 0;
        }

        sendUnityEvent("done", uttId);

        if (left == 0) sendUnityEvent("done_any", uttId);
    }

    private void onListenerError(String uttId) {
        forceDone(uttId, true);
    }

    private void sendUnityEvent(String kind, String payload) {
        String go = unityGameObject;
        String m = unityCallbackMethod;

        if (go == null || go.isEmpty() || m == null || m.isEmpty())
            return;

        String msg = kind + "|" + (payload == null ? "" : payload);

        try {
            UnityPlayer.UnitySendMessage(go, m, msg);
        } catch (Throwable ignored) {
        }
    }

    private static long computeDeadlineMs(String text) {
        int len = text.length();

        long base = 1200L;
        long perChar = 55L;

        long est = base + (long) len * perChar;

        if (len <= 12) est = Math.max(est, 2200L);
        est = Math.max(est, 2500L);

        est = Math.min(est, 30000L);
        return est;
    }

    private static String normalizeTag(String languageTag) {
        String t = (languageTag == null) ? "" : languageTag.trim();
        if (t.isEmpty()) t = "en";
        t = t.replace('_', '-');
        return t;
    }

    private static Locale toLocaleCompat(String tag) {
        String t = normalizeTag(tag);

        int dash = t.indexOf('-');
        if (dash < 0) {
            return new Locale(t);
        }

        String lang = t.substring(0, dash);
        String rest = t.substring(dash + 1);

        int dash2 = rest.indexOf('-');
        if (dash2 >= 0) rest = rest.substring(0, dash2);

        if (rest.isEmpty())
            return new Locale(lang);

        return new Locale(lang, rest);
    }

    private static String localeToTagCompat(Locale l) {
        if (l == null) return "";
        try {
            if (Build.VERSION.SDK_INT >= 21) {
                String t = l.toLanguageTag();
                return t == null ? "" : t;
            }
        } catch (Throwable ignored) {
        }

        String lang = safe(l.getLanguage());
        String country = safe(l.getCountry());

        if (lang.isEmpty()) return "";
        if (country.isEmpty()) return lang;
        return lang + "-" + country;
    }

    private static String safe(String s) {
        return s == null ? "" : s.trim();
    }

    private final class Listener extends UtteranceProgressListener {
        @Override
        public void onStart(String utteranceId) {
            onListenerStart(utteranceId);
        }

        @Override
        public void onDone(String utteranceId) {
            onListenerDone(utteranceId);
        }

        @Override
        public void onError(String utteranceId) {
            onListenerError(utteranceId);
        }

        @Override
        public void onError(String utteranceId, int errorCode) {
            onListenerError(utteranceId);
        }
    }
}