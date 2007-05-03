#pragma once
#include <vector>
#include <string>
#include <iobinaryReader>
#include <utility>
#include <wx/colour.h>
class TileGraphic;
typedef unsigned int GLuint;
#define TypeTile 0
#define TypeObject 1

class TileLoader {
public:
	static uint32 numTiles[2];
	void Init();
	GLuint Load(std::pair<uint32, int> tileIdentifier, uint32 &width, uint32 &height);
private:
	static uint32 numArchives[2];
	static const char *typeNames[2];
	char *dataPath;
	struct ArchiveFile {
		uint32 offset;
		char name[13];
		// Conform to EqualityComparable so that we can use std::find
		bool operator ==(const char *compare);
		template<class T> inline bool operator !=(T &compare) {
			return !(*this == compare); }
	};
	struct ArchiveHeader {
		std::vector<ArchiveFile> files; };
	uint32 ArchiveFileSize(std::ibinaryReader &in, ArchiveHeader &header, const char *fileName);
	void ArchiveSeek(std::ibinaryReader &in, ArchiveHeader &header, const char *fileName);
	struct GraphicsHeader { // The header for an EPF file
		uint16 tileCount, // The number of tiles contained in this file
			frameHeight, frameWidth, unknown; // Ignored values
		uint32 infoOffset; // Tile information is located after this offset
	};
	// A slimmed down version of GraphicsHeader that contains only the necessary information
	struct GraphicsFile {
		uint16 tileCount;
		uint32 infoOffset;
		GraphicsFile(GraphicsHeader &copy) :
			tileCount(copy.tileCount), infoOffset(copy.infoOffset) { }
	};
	struct GraphicsTileInfo { // Information about a tile in an EPF file
		uint16 left, top, right, bottom; // The dimensions of the tile
		uint32 GetWidth() { return right - left; }
		uint32 GetHeight() { return bottom - top; }
		// Between these offsets lies the actual tile data!
		uint32 startOffset, endOffset;
	};
	std::vector<GraphicsFile> graphicsFiles[2];
	typedef std::vector<uint8> PaletteTable;
	struct Palette { wxColor data[256]; };
	typedef std::vector<Palette> PaletteSet;
	PaletteTable paletteTables[2];
	PaletteSet paletteSets[2];

	friend std::ibinaryReader &operator >>(std::ibinaryReader &, ArchiveHeader &);
	friend std::ibinaryReader &operator >>(std::ibinaryReader &, PaletteSet &);
	friend std::ibinaryReader &operator >>(std::ibinaryReader &, PaletteTable &);
};
std::ibinaryReader &operator >>(std::ibinaryReader &in, TileLoader::ArchiveHeader &header);
std::ibinaryReader &operator >>(std::ibinaryReader &in, TileLoader::PaletteSet &set);
std::ibinaryReader &operator >>(std::ibinaryReader &in, TileLoader::PaletteTable &table);

extern TileLoader tileLoader;