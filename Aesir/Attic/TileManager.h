#pragma once
typedef unsigned int GLuint;
#include <map>
#include <utility>
#include <vector>
#include <wx/timer.h>
#define TypeTile 0
#define TypeObject 1

/* Information that describes a tile. Note that you shouldn't access this class
 * directly; only use a TileHandle. */
class TileGraphic {
public:
	uint32 index;
	int tileType; // The type of the tile (Either TypeTile or TypeObject)
	GLuint texture; // The GL texture
	uint32 width, height; // The dimensions of the tile
	~TileGraphic(); // Destructor frees the GL texture and unregisters the graphic
	void Render(int x, int y);
private:
	typedef std::map<uint32, TileGraphic *>::iterator InternalHandle;
	InternalHandle internalHandle;
	inline TileGraphic(uint32 index_, int tileType_) :
		index(index_), tileType(tileType_), refcount(0) { }
	friend class TileManager; // For access to ctor
	friend class TileHandle; // For access to the refcount
	uint32 refcount; // For refcounted resource management via TileHandle
};
// A handle to a tile...
class TileHandle {
public:
	// TODO: I probably should throw some asserts in here...
	inline TileHandle(const TileHandle &copy) {
		tileGraphic = copy.tileGraphic;
		if(tileGraphic) ++tileGraphic->refcount;
	}
	inline TileHandle &operator =(const TileHandle &assign) {
		TileHandle::~TileHandle();
		tileGraphic = assign.tileGraphic;
		if(tileGraphic) ++tileGraphic->refcount;
		return *this;
	}
	inline TileHandle() : tileGraphic(0) { }
	inline operator bool() { return (tileGraphic != 0); }
	inline TileGraphic &operator *() { return *tileGraphic; }
	inline TileGraphic *operator ->() { return tileGraphic; }
	inline operator TileGraphic *() { return tileGraphic; }
	inline ~TileHandle();
private:
	inline TileHandle(TileGraphic *tileGraphic_) :
		tileGraphic(tileGraphic_) {
			if(tileGraphic) ++tileGraphic->refcount; }
	TileGraphic *tileGraphic;
	friend class TileManager; // For access to ctor
};
// Manages tiles! Duh!
class TileManager : public wxEvtHandler {
public:
	// Every FLUSH_INTERVAL milliseconds, Flush() will be called
	static const int FLUSH_INTERVAL = 1500;
	// Request a tile; if the tile is not loaded, load the tile
	TileHandle Request(uint32 index, int tileType);
	void Flush(); // Empty the deletionQueue, destroying everything
	~TileManager();
	inline TileManager() : flushTimer(this, 0) {
		flushTimer.Start(FLUSH_INTERVAL); }
private:
	inline void OnFlushNotify(wxTimerEvent &) { Flush(); }
	wxTimer flushTimer;
	typedef std::map<uint32, TileGraphic *>::iterator InternalHandle;
	std::map<uint32, TileGraphic *> tiles[2];
	typedef std::vector<TileGraphic *> DeletionQueue;
	DeletionQueue deletionQueue;
	friend class TileGraphic; // For access to the tiles map
	friend class TileHandle; // For access to the deletion queue
	DECLARE_EVENT_TABLE()
};
extern TileManager tileManager;
TileHandle::~TileHandle() {
	if(tileGraphic && --tileGraphic->refcount <= 0)
		tileManager.deletionQueue.push_back(tileGraphic);
}