#include "stdwx.h"
#include "MapView.h"
#include "MapEditor.h"
#include "BasicCanvas.h"
#include "TileManager.h"
#include <wx/docmdi.h>
IMPLEMENT_DYNAMIC_CLASS(MapView, wxView)

class MapView::GraphicsCanvas : public BasicCanvas {
public:
	inline GraphicsCanvas(wxWindow *parent) : BasicCanvas(parent) { }
	void Render();
};
class MapView::ChildFrame : public wxDocMDIChildFrame {
public:
	ChildFrame(wxDocument *doc, MapView *mapView_, wxMDIParentFrame *parent);
private:
	GraphicsCanvas *graphicsCanvas;
	wxScrollBar *scrollHoriz, *scrollVert;
	MapView *mapView;
	void OnSize(wxSizeEvent &event);
	DECLARE_EVENT_TABLE()
};

BEGIN_EVENT_TABLE(MapView::ChildFrame, wxDocMDIChildFrame)
	EVT_SIZE(MapView::ChildFrame::OnSize)
END_EVENT_TABLE()

MapView::ChildFrame::ChildFrame(wxDocument *doc, MapView *mapView_, wxMDIParentFrame *parent) :
	wxDocMDIChildFrame(doc, mapView_, parent, -1, ""), mapView(mapView_) {
	this->Show();
	graphicsCanvas = new GraphicsCanvas(this);
}
void MapView::ChildFrame::OnSize(wxSizeEvent &event) { }
void MapView::GraphicsCanvas::Render() {
	this->SetCurrent();
	glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
	this->SwapBuffers();
}
bool MapView::OnCreate(wxDocument *doc, long flags) {
	this->SetFrame(new ChildFrame(doc, this, mainFrame));
	return true;
}
bool MapView::OnClose(bool deleteWindow) {
	if(!GetDocument()->Close()) return false;
	this->Activate(false);
	if(deleteWindow) delete GetFrame();
	return true;
}