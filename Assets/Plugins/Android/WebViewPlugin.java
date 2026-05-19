package com.ustaldanquiz.plugin;

import android.app.Activity;
import android.graphics.Color;
import android.graphics.Typeface;
import android.util.TypedValue;
import android.view.Gravity;
import android.view.View;
import android.view.ViewGroup;
import android.webkit.WebSettings;
import android.webkit.WebView;
import android.webkit.WebViewClient;
import android.widget.FrameLayout;
import android.widget.TextView;

public class WebViewPlugin {
    private FrameLayout container;
    private WebView     webView;
    private Activity    activity;
    private String      callbackGO;

    public WebViewPlugin(Activity a, String gameObjectName) {
        activity   = a;
        callbackGO = gameObjectName;
    }

    public void Show(final String url, final int headerPx) {
        activity.runOnUiThread(new Runnable() {
            @Override public void run() {
                if (container == null) buildContainer(headerPx);

                // update header + webview top margin if called again
                container.getChildAt(0).getLayoutParams().height = headerPx;
                ((FrameLayout.LayoutParams) webView.getLayoutParams()).topMargin = headerPx;

                webView.loadUrl(url);
                container.setVisibility(View.VISIBLE);
            }
        });
    }

    public void Hide() {
        if (container == null) return;
        activity.runOnUiThread(new Runnable() {
            @Override public void run() {
                if (container != null) container.setVisibility(View.GONE);
            }
        });
    }

    public void Destroy() {
        if (container == null) return;
        activity.runOnUiThread(new Runnable() {
            @Override public void run() {
                if (container == null) return;
                ViewGroup p = (ViewGroup) container.getParent();
                if (p != null) p.removeView(container);
                if (webView != null) { webView.destroy(); webView = null; }
                container = null;
            }
        });
    }

    // ── приватные ────────────────────────────────────────────────────────────

    private void buildContainer(int headerPx) {
        container = new FrameLayout(activity);

        // Шапка (зелёный фон C_PRIMARY)
        FrameLayout header = new FrameLayout(activity);
        header.setBackgroundColor(Color.rgb(45, 96, 64));
        FrameLayout.LayoutParams hLP = new FrameLayout.LayoutParams(
            ViewGroup.LayoutParams.MATCH_PARENT, headerPx);
        hLP.gravity = Gravity.TOP;
        container.addView(header, hLP);

        // Кнопка ✕
        TextView closeBtn = new TextView(activity);
        closeBtn.setText("✕");
        closeBtn.setTextColor(Color.WHITE);
        closeBtn.setTextSize(TypedValue.COMPLEX_UNIT_PX, headerPx * 0.42f);
        closeBtn.setGravity(Gravity.CENTER);
        closeBtn.setTypeface(null, Typeface.BOLD);
        closeBtn.setOnClickListener(new View.OnClickListener() {
            @Override public void onClick(View v) {
                com.unity3d.player.UnityPlayer.UnitySendMessage(callbackGO, "OnNativeClose", "");
            }
        });
        int btnW = (int)(headerPx * 1.2f);
        FrameLayout.LayoutParams cLP = new FrameLayout.LayoutParams(btnW, ViewGroup.LayoutParams.MATCH_PARENT);
        cLP.gravity = Gravity.START;
        header.addView(closeBtn, cLP);

        // Заголовок
        TextView titleTV = new TextView(activity);
        titleTV.setText("100.uacbs.ru");
        titleTV.setTextColor(Color.WHITE);
        titleTV.setTextSize(TypedValue.COMPLEX_UNIT_PX, headerPx * 0.36f);
        titleTV.setGravity(Gravity.CENTER);
        header.addView(titleTV, new FrameLayout.LayoutParams(
            ViewGroup.LayoutParams.MATCH_PARENT,
            ViewGroup.LayoutParams.MATCH_PARENT));

        // WebView
        webView = new WebView(activity);
        WebSettings ws = webView.getSettings();
        ws.setJavaScriptEnabled(true);
        ws.setDomStorageEnabled(true);
        webView.setWebViewClient(new WebViewClient());

        FrameLayout.LayoutParams wLP = new FrameLayout.LayoutParams(
            ViewGroup.LayoutParams.MATCH_PARENT,
            ViewGroup.LayoutParams.MATCH_PARENT);
        wLP.topMargin = headerPx;
        container.addView(webView, wLP);

        activity.addContentView(container, new FrameLayout.LayoutParams(
            ViewGroup.LayoutParams.MATCH_PARENT,
            ViewGroup.LayoutParams.MATCH_PARENT));
    }
}
