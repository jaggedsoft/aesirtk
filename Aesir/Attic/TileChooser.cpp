#include "stdwx.h"
#include "TileChooser.h"
#include "TileLoader.h"
#include "MapEditor.h"
#include <cmath>
#include <boost/array.hpp>
#include <algorithm>
using namespace std;

class TileChooser::GraphicsCanvas : public BasicCanvas {
public:
	GraphicsCanvas(TileChooser *tileChooser_);
	void Render();
private:
	inline void HandleMiddleDrag(wxMouseEvent &event) {
		tileChooser->HandleMiddleDrag(event); }
	inline void OnMotion(wxMouseEvent &event) {
		tileChooser->HandleMiddleDrag(event);
		tileChooser->HandleSelectionDrag(event);
	}
	inline void HandleSelectionDrag(wxMouseEvent &event) {
		tileChooser->HandleSelectionDrag(event); }
	TileChooser *tileChooser;
	DECLARE_EVENT_TABLE()
};

BEGIN_EVENT_TABLE(TileChooser::GraphicsCanvas, BasicCanvas)
	EVT_MOTION(TileChooser::GraphicsCanvas::OnMotion)
	EVT_LEFT_DOWN(TileChooser::GraphicsCanvas::HandleSelectionDrag)
	EVT_LEFT_UP(TileChooser::GraphicsCanvas::HandleSelectionDrag)
	EVT_MIDDLE_DOWN(TileChooser::GraphicsCanvas::HandleMiddleDrag)
	EVT_MIDDLE_UP(TileChooser::GraphicsCanvas::HandleMiddleDrag)
END_EVENT_TABLE()
BEGIN_EVENT_TABLE(TileChooser, wxPanel)
	EVT_SIZE(TileChooser::OnSize)
	EVT_SCROLL(TileChooser::OnScroll)
	EVT_MOUSEWHEEL(TileChooser::OnMouseWheel)
END_EVENT_TABLE()

void TileChooser::HandleSelectionDrag(wxMouseEvent &event) {
	wxPoint mousePos = event.GetPosition();
	this->ScreenToClient(&mousePos.x, &mousePos.y);
	if(event.LeftDown()) {
		// The user has begin dragging a selection box
		selectOrigin = mousePos;
		boost::array<int, 3> extents = { 0, 0 };
		mapEditor->selection.reshape(extents);
	} else if(event.LeftUp()) {
		// The user has stopped dragging a selection box
	} else if(event.LeftIsDown()) {
		// The user is currently dragging a selection box
		wxPoint delta = (mousePos - selectOrigin);
		wxSize selectSize(
			float(selectOrigin.x), 0
		);
		/*return GetTileOffset() + (point.x / tileSize) +
		(int(point.y + scrollInterp * tileSize) / tileSize) * ringBuffer.GetWidth();*/
	}
}
TileChooser::TileChooser(wxWindow *parent) : wxPanel(parent), scrollDisplacement(0), scrollInterp(0) {
	graphicsCanvas = new GraphicsCanvas(this);
	scrollVert = new wxScrollBar(this, -1, wxDefaultPosition, wxDefaultSize, wxVERTICAL);
	toolBar = new wxToolBar(this, -1, wxDefaultPosition, wxDefaultSize,
		wxTB_TEXT | wxTB_HORIZONTAL | wxTB_NOICONS | wxBORDER_NONE);
	toolBar->AddTool(0, "Zoom In", wxNullBitmap, wxNullBitmap, wxITEM_NORMAL, "Decrease the viewing area");
	toolBar->AddTool(1, "Zoom Out", wxNullBitmap, wxNullBitmap, wxITEM_NORMAL, "Enlarge the viewing area");
	toolBar->Realize();
	SetSize(parent->GetClientSize()); // TEMP?
}
void TileChooser::OnMouseWheel(wxMouseEvent &event) {
	scrollVert->SetThumbPosition(scrollVert->GetThumbPosition() - event.GetWheelRotation() / 2);
	UpdateScroll();
}
void TileChooser::Rebuild() {
	for(int y = 0; y < ringBuffer.GetHeight(); ++y) {
		for(int x = 0; x < ringBuffer.GetWidth(); ++x) {
			int index = GetTileOffset() + x + y * ringBuffer.GetWidth();
			ringBuffer[y][x] = tileManager.Request(index, TypeTile);
		}
	}
}
void TileChooser::UpdateScroll() {
	int deltaPos = (scrollVert->GetThumbPosition() - (scrollDisplacement * tileSize)), advance = 0;
	scrollInterp = float(deltaPos) / float(tileSize);
	if(scrollInterp > 1) advance = 1;
	if(scrollInterp < 0) advance = -1;
	if(abs(scrollInterp) > ringBuffer.GetHeight()) {
		scrollDisplacement += advance * floor(scrollInterp);
		scrollInterp -= floor(scrollInterp);
		Rebuild();
	} else {
		while(scrollInterp > 1 || scrollInterp < 0) {
			scrollInterp += -advance;
			scrollDisplacement += advance;
			ringBuffer.Advance(advance);
			TileHandle *row = (advance > 0)?ringBuffer.GetBack():ringBuffer.GetFront();
			for(int x = 0; x < ringBuffer.GetWidth(); ++x) {
				int index = GetTileOffset() + x;
				if(advance > 0) index += ringBuffer.GetWidth() * (ringBuffer.GetHeight() - 1);
				row[x] = tileManager.Request(index, TypeTile);
			}
		}
	}
	graphicsCanvas->Render();
}
uint32 TileChooser::HitTest(wxPoint point) {
	return GetTileOffset() + (point.x / tileSize) +
		(int(point.y + scrollInterp * tileSize) / tileSize) * ringBuffer.GetWidth();
}
void TileChooser::OnSize(wxSizeEvent &event) {
	int width = event.GetSize().GetWidth(), height = event.GetSize().GetHeight();
	// Manually size everything...
	scrollVert->SetSize(width - 16, 0, 16, height - 24);
	graphicsCanvas->SetSize(0, 0, width - 16, height - 24);
	toolBar->SetSize(0, height - 24, width, 24);
	int tileOffset_ = GetTileOffset();
	ringBuffer.Reshape(
		floor(float(graphicsCanvas->GetSize().GetWidth()) / float(tileSize)),
		ceil(float(graphicsCanvas->GetSize().GetHeight()) / float(tileSize)) + 1
	);
	scrollDisplacement = tileOffset_ / ringBuffer.GetWidth();
	scrollVert->SetScrollbar(scrollDisplacement * tileSize,
		float(ringBuffer.GetHeight() * ringBuffer.GetWidth()) / (float)tileLoader.numTiles[TypeTile],
		(tileLoader.numTiles[TypeTile] / ringBuffer.GetWidth()) * tileSize,
		tileSize);
	Rebuild();
	graphicsCanvas->Render();
}
void TileChooser::HandleMiddleDrag(wxMouseEvent &event) {
	if(event.MiddleDown()) {
		draggingMousePos = event.GetPosition();
		draggingIgnoreEvent = false;
		// Change the cursor into an up-down arrow to indicate dragging
		this->SetCursor(wxCURSOR_SIZENS);
	} else if(event.MiddleUp()) {
		// We've stopped dragging, reset the cursor!
		this->SetCursor(wxNullCursor);
	} else if(event.MiddleIsDown()) {
		if(!draggingIgnoreEvent) {
			scrollVert->SetThumbPosition(scrollVert->GetThumbPosition() -
				(event.GetPosition().y - draggingMousePos.y) * 5);
			UpdateScroll();
			this->WarpPointer(draggingMousePos.x, draggingMousePos.y);
		}
		draggingIgnoreEvent = !draggingIgnoreEvent;
	}
}
void TileChooser::GraphicsCanvas::Render() {
	this->SetCurrent();
	glClear(GL_COLOR_BUFFER_BIT | GL_DEPTH_BUFFER_BIT);
	glPushMatrix();
	glTranslatef(0, -tileChooser->scrollInterp * tileSize, 0);
	TileGraphic *tileGraphic = 0;
	for(int y = 0; y < tileChooser->ringBuffer.GetHeight(); ++y) {
		for(int x = 0; x < tileChooser->ringBuffer.GetWidth(); ++x) {
			tileGraphic = tileChooser->ringBuffer[y][x];
			if(find(mapEditor->selection.begin(), mapEditor->selection.end(),
				make_pair<uint32, int>(tileGraphic->index, TypeTile)) != mapEditor->selection.end())
				glColor3f(0.5, 0.5, 0.5);
			else glColor3f(1, 1, 1);
			tileGraphic->Render(x * tileSize, y * tileSize);
		}
	}
	glPopMatrix();
	this->SwapBuffers();
}
#if 0
void TileChooser::GraphicsCanvas::OnMotion(wxMouseEvent &event) {
	wxPoint mousePos = wxGetMousePosition();
	this->ScreenToClient(&mousePos.x, &mousePos.y);
	uint32 mouseOver_ = tileChooser->HitTest(mousePos);
	if(mouseOver_ != mouseOver) {
		mouseOver = mouseOver_;
		Render();
	}
	// HACK: Let the tile chooser interpret the event for dragging
	tileChooser->HandleMiddleDrag(event);
}
#endif
TileChooser::GraphicsCanvas::GraphicsCanvas(TileChooser *tileChooser_) :
	BasicCanvas(tileChooser_, true), tileChooser(tileChooser_) { }