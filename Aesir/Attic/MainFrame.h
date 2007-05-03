#pragma once
class wxGLCanvas;
#include <wx/docview.h>
#include <wx/docmdi.h>
class TileChooser;

class MainFrame : public wxDocMDIParentFrame {
public:
	MainFrame();
	void OnSize(wxSizeEvent &event);
	DECLARE_EVENT_TABLE()
private:
	TileChooser *tileChooser;
};