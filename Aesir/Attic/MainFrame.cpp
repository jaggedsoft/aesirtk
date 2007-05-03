#include "stdwx.h"
#include "MainFrame.h"
#include "TileLoader.h"
#include "BasicCanvas.h"
#include "MapEditor.h"
#include <gl/gl.h>
#include "TileChooser.h"
#include <exception>
using std::exception;

BEGIN_EVENT_TABLE(MainFrame, wxDocMDIParentFrame)
	EVT_SIZE(MainFrame::OnSize)
END_EVENT_TABLE()

void MainFrame::OnSize(wxSizeEvent &event) {
	int width, height;
	this->GetClientSize(&width, &height);
	int tileSize = (48 + 2) * 4 + 16;
	this->m_clientWindow->SetSize(tileSize, 0, width - tileSize, height);
	tileChooser->SetSize(0, 0, tileSize, height);
}
MainFrame::MainFrame() : wxDocMDIParentFrame(docManager, 0, -1, "MapEditor") {
	wxMenuBar *menuBar = new wxMenuBar();
	wxMenu *menuFile = new wxMenu();
	menuFile->Append(wxID_NEW, "&New");
	menuFile->Append(wxID_OPEN, "&Open");
	menuFile->Append(wxID_EXIT, "&Exit");
	menuBar->Append(menuFile, "&File");
	this->SetMenuBar(menuBar);
	// TODO: Move this into the application class
	try { tileLoader.Init(); } // TODO: Better error handling
	catch(exception &e) { wxMessageBox(e.what()); }
	tileChooser = new TileChooser(this);
	this->SetSize(100, 100, 600, 600); // TEMP
	this->Show();
}