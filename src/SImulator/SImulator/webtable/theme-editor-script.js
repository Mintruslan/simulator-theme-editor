try {
	sigame.runThemeEditor('themeEditorHost');
	window.chrome?.webview?.postMessage({ type: 'loaded' });
} catch (error) {
	window.chrome?.webview?.postMessage({ type: 'loadError', error: String(error) });
}
