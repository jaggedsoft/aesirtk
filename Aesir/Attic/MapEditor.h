#pragma once
class wxGLContext;
typedef unsigned int GLuint;
class wxDocManager;
class wxMDIParentFrame;
#include <boost/multi_array.hpp>
typedef std::pair<uint32, int> TileIdentifier;

class MapEditor : public wxApp {
public:
	virtual bool OnInit();
	virtual int OnExit();

	boost::multi_array<TileIdentifier, 2> selection;
};

// TODO: Move these into the application class!
extern wxGLContext *mainContext;
extern wxMDIParentFrame *mainFrame;
extern wxDocManager *docManager;
extern MapEditor *mapEditor;