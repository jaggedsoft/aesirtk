#pragma once
#include <wx/glcanvas.h>

class BasicCanvas : public wxGLCanvas {
public:
	BasicCanvas(wxWindow *parent);
	// HACK: This is a pretty shitty way to do it...
	BasicCanvas(wxWindow *parent, bool main);
	// Derived classes override this to implement custom drawing
	virtual void Render() { }
private:
	wxScrollBar *horizontalScroll, *verticalScroll;
	void Resize(int width, int height);
	void Setup();
	void OnSize(wxSizeEvent &event);
	void OnPaint(wxPaintEvent &event);
	DECLARE_EVENT_TABLE()
};