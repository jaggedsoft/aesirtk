#include "stdwx.h"
#include "TileManager.h"
#include "TileLoader.h"
#include <gl/gl.h>
#include <utility>
#include <algorithm>
#include <boost/bind.hpp>
#if 0
#include <boost/thread/thread.hpp>
#include <boost/thread/condition.hpp>
#include <boost/thread/mutex.hpp>
#endif
using namespace boost;
using namespace std;
TileManager tileManager;

BEGIN_EVENT_TABLE(TileManager, wxEvtHandler)
	EVT_TIMER(0, OnFlushNotify)
END_EVENT_TABLE()

TileGraphic::~TileGraphic() {
	if(texture)
		glDeleteTextures(1, &texture);
	tileManager.tiles[tileType].erase(internalHandle);
}
void TileGraphic::Render(int x, int y) {
	glBindTexture(GL_TEXTURE_2D, texture);
	glBegin(GL_QUADS);
		glTexCoord2f(0, 0); glVertex2i(0 + x, 0 + y);
		glTexCoord2f(1, 0); glVertex2i(48 + x, 0 + y);
		glTexCoord2f(1, 1); glVertex2i(48 + x, 48 + y);
		glTexCoord2f(0, 1); glVertex2i(0 + x, 48 + y);
	glEnd();
	glBindTexture(GL_TEXTURE_2D, 0);
}
void TileManager::Flush() {
	while(deletionQueue.size() > 0) {
		TileGraphic *tileGraphic = deletionQueue.back();
		if(tileGraphic->refcount <= 0)
			delete tileGraphic;
		deletionQueue.pop_back();
	}
}
TileHandle TileManager::Request(uint32 index, int tileType) {
	InternalHandle internalHandle = tiles[tileType].find(index);
	TileGraphic *tileGraphic;
	if(internalHandle == tiles[tileType].end()) {
		tileGraphic = new TileGraphic(index, tileType);
		tileGraphic->internalHandle = tiles[tileType].insert(make_pair(index, tileGraphic)).first;
		tileGraphic->texture = tileLoader.Load(make_pair(index, tileType),
			tileGraphic->width, tileGraphic->height);
	} else tileGraphic = internalHandle->second;
	return TileHandle(tileGraphic);
}
TileManager::~TileManager() {
	for(int i = 0; i < 2; ++i) {
		while(tiles[i].size() != 0)
			delete tiles[i].begin()->second;
	}
}

#if 0
// Abandoned code for an asynchronous loading component
typedef std::pair<uint32, int> TileIdentifier;
class TileManager::AsyncLoader {
public:
	AsyncLoader(TileManager *tileManager);
	~AsyncLoader();
	void Load(TileIdentifier tileIdentifier);
private:
	void LoadingThread();
	thread loadingThread;
	TileManager *tileManager;
	condition pendingEvents;
	mutex pendingLoadsMutex;
	deque<TileIdentifier> pendingLoads;
	mutex killMutex;
	bool kill;
};
TileManager::AsyncLoader::AsyncLoader(TileManager *tileManager_) :
	kill(false), tileManager(tileManager_),
	loadingThread(bind(&TileManager::AsyncLoader::LoadingThread, this)) { }
void TileManager::AsyncLoader::Load(TileIdentifier tileIdentifier) {
	{
		mutex::scoped_lock lock(pendingLoadsMutex);
		pendingLoads.push_back(tileIdentifier);
	}
}
TileManager::AsyncLoader::~AsyncLoader() {
	{
		// Kill the loading thread
		mutex::scoped_lock lock(killMutex);
		kill = true;
		pendingEvents.notify_all();
	}
	loadingThread.join();
}
void TileManager::AsyncLoader::LoadingThread() {
	while(true) {
		{
			// See if we should terminate the thread
			mutex::scoped_lock lock(killMutex);
			if(kill) return;
		}
		TileIdentifier loadingTile;
		{
			// Get a tile to load from the deque
			mutex::scoped_lock lock(pendingLoadsMutex);
			if(pendingLoads.size() == 0)
				pendingEvents.wait(lock);
			{
				// We may have been awakened to exit the thread
				mutex::scoped_lock lock(killMutex);
				if(kill) return;
			}
			loadingTile = pendingLoads.front();
			pendingLoads.pop_front();
		}
		int width, height;
		GLuint texture;
		texture = tileLoader.Load(loadingTile, width, height);
	}
}
#endif