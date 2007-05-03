#include "stdwx.h"
#include "BasicCanvas.h"
#include "MapEditor.h"
#include <gl/gl.h>
#include <gl/glu.h>

BEGIN_EVENT_TABLE(BasicCanvas, wxGLCanvas)
	EVT_SIZE(BasicCanvas::OnSize)
	EVT_PAINT(BasicCanvas::OnPaint)
END_EVENT_TABLE()

BasicCanvas::BasicCanvas(wxWindow *parent) :
	wxGLCanvas(parent, mainContext) { Setup(); }
BasicCanvas::BasicCanvas(wxWindow *parent, bool main) :
	wxGLCanvas(parent) {
	mainContext = this->GetContext();
	Setup();
}
void BasicCanvas::Setup() {
	this->SetCurrent();
	glClearColor(0, 0, 0, 0);
	glEnable(GL_TEXTURE_2D);
}
void BasicCanvas::Resize(int width, int height) {
	this->SetCurrent();
	glMatrixMode(GL_PROJECTION);
	glLoadIdentity();
	glViewport(0, 0, width, height);
	gluOrtho2D(0, width, height, 0);
}

void BasicCanvas::OnSize(wxSizeEvent &event) {
	Resize(event.GetSize().GetWidth(), event.GetSize().GetHeight()); }
void BasicCanvas::OnPaint(wxPaintEvent &event) {
	wxPaintDC(this);
	this->Render();
}