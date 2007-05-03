#include "stdwx.h"
#include "MapEditor.h"
#include "MainFrame.h"
#include <wx/docview.h>
#include "MapDocument.h"
#include "MapView.h"
wxGLContext *mainContext = 0;
wxMDIParentFrame *mainFrame = 0;
wxDocManager *docManager = 0;
MapEditor *mapEditor = 0;

bool MapEditor::OnInit() {
	mapEditor = this;
	docManager = new wxDocManager();
	new wxDocTemplate(docManager, "Nexus Map", "*.nme", "", "nme",
		"Map Document", "Map View", CLASSINFO(MapDocument), CLASSINFO(MapView));
	mainFrame = new MainFrame();
	docManager->CreateDocument("", wxDOC_NEW);
	this->SetTopWindow(mainFrame);
	return true;
}
int MapEditor::OnExit() {
	delete docManager;
	return 0;
}

IMPLEMENT_APP(MapEditor);