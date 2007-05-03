#pragma once
#include <wx/docview.h>

class MapDocument : public wxDocument {
	DECLARE_DYNAMIC_CLASS(MapDocument)
public:
	static const int QUADRANT_SIZE = 256;
	class Quadrant;
	wxOutputStream &SaveObject(wxOutputStream &stream) { return stream; }
	wxInputStream &LoadObject(wxInputStream &stream) { return stream; }
	void InsertTile(int x, int y, int tileIndex);
private:
	Quadrant *root;
	wxPoint top_left;
	wxSize size;
};