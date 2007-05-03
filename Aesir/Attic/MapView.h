#pragma once
#include <wx/docview.h>

class MapView : public wxView {
	DECLARE_DYNAMIC_CLASS(MapView)
public:
	bool OnCreate(wxDocument *doc, long flags);
	void OnUpdate(wxView *sender, wxObject *hint) { } // TODO
	void OnDraw(wxDC *dc) { } // TODO
	bool OnClose(bool deleteWindow);
private:
	friend class GraphicsCanvas;
	friend class ChildFrame;
	class GraphicsCanvas;
	class ChildFrame;
};