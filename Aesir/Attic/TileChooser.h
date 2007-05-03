#pragma once
#include <deque>
#include <vector>
#include <cmath>
#include "BasicCanvas.h"
#include "TileManager.h"

/* BUG: When you scroll to the end and sometimes randomly all the tiles turn white
 * and scrollDisplacement goes off of the map! The bug only occurs when you are
 * dragging the thumb; not when you are scrolling by other means. */

class TileChooser : public wxPanel {
public:
	TileChooser(wxWindow *parent);
private:
	static const int tilePadding = 2; // Padding between displayed tiles
	static const int tileSize = 48 + tilePadding; // Total tile size
	friend class GraphicsCanvas;
	class GraphicsCanvas;
	/* A buffer which stores rows of tile handles that allows you to move the "head" of the
	 * buffer -- for example, if you advance the head by one and then attempt to access index
	 * 0, you will really be accessing the data at absolute index 1. Also, indices wrap around
	 * accordingly. This data structure is used to store tile references for smooth scrolling. */
	class RingBuffer {
	public:
		// Returns a pointer to a row, given a relative index
		inline TileHandle *operator [](int row) {
			return buffer + ((row + head) % GetHeight()) * GetWidth();
		}
		inline RingBuffer() : buffer(0), head(0) { }
		inline ~RingBuffer() { delete[] buffer; }
		// Reallocate the ring buffer, changing the dimensions
		inline void Reshape(int width, int height) {
			delete[] buffer;
			buffer = new TileHandle[width * height];
			size.Set(width, height);
			head = 0;
		}
		// The width is the number of columns; that is, elements in a row
		inline int GetWidth() { return size.GetWidth(); }
		// The height is the number of rows
		inline int GetHeight() { return size.GetHeight(); }
		// Advance the head index by a certain amount
		inline void Advance(int amount) {
			head += amount;
			// HACK: Make positive, since mod doesn't like negatives
			while(head < 0) head += GetHeight();
			head %= GetHeight();
		}
		// Reset the position of the head
		inline void Reset(int head_ = 0) { head = head_; }
		// Get the pointer to the "first" (relative) row in a ringbuffer
		inline TileHandle *GetFront() { return (*this)[0]; }
		// Get the pointer to the "last" (relative) row in a ringbuffer
		inline TileHandle *GetBack() { return (*this)[GetHeight() - 1]; }
	private:
		wxSize size;
		int head;
		TileHandle *buffer;
	};
	RingBuffer ringBuffer;
	uint32 HitTest(wxPoint point); // Performs a hit test and returns a tile index
	void Rebuild(); // Completely rebuild the ring buffer!
	/* The last "whole" scroll position since an EVT_SCROLL (used to obtain a delta).
	 * a "whole" scroll position is a position that is a multiple of tileSize. */
	int scrollDisplacement; // The scrolling vertical offset from the top in tiles
	float scrollInterp; // Used for smooth scrolling
	GraphicsCanvas *graphicsCanvas;
	wxScrollBar *scrollVert; // The vertical scroll bar
	wxPoint draggingMousePos; // The position where the mouse started dragging, if we are dragging
	bool draggingIgnoreEvent; // Warping the mouse while dragging generates an event we must ignore
	wxToolBar *toolBar; // TODO: Something better than a tool bar for this?
	// TODO: Implement the zoom feature
	/* int zoomLevel; */
	wxPoint selectOrigin; // The point where the user first started dragging a selection box
	void UpdateScroll(); // Update the data from the position of the scroll bar
	void HandleMiddleDrag(wxMouseEvent &event); // For dragging using the middle mouse button
	void HandleSelectionDrag(wxMouseEvent &event); // For selecting things by dragging the mouse
	void OnMouseWheel(wxMouseEvent &event); // For scrolling using the mouse wheel
	inline void OnScroll(wxScrollEvent &event) { UpdateScroll(); }
	void OnSize(wxSizeEvent &event); // Resize the control manually!
	// Get the index of the first tile displayed within the tile chooser, based on the scroll displacement
	inline int GetTileOffset() { return scrollDisplacement * ringBuffer.GetWidth(); }
	DECLARE_EVENT_TABLE()
};