#include "stdwx.h"
#include <wx/glcanvas.h>
#include "TileLoader.h"
#include "MapEditor.h"
#include <sstream>
#include <fstream>
#include <algorithm>
#include <gl/gl.h>
#include <gl/glu.h>
#include <boost/format.hpp>
#include <boost/lexical_cast.hpp>
using namespace boost;
using namespace std;
TileLoader tileLoader;
uint32 TileLoader::numArchives[2] = { 16, 18 };
const char *TileLoader::typeNames[2] = { "tile", "tilec" };
uint32 TileLoader::numTiles[2] = { 0, 0 };

GLuint TileLoader::Load(pair<uint32, int> tileIdentifier, uint32 &width, uint32 &height) {
	uint32 index = tileIdentifier.first;
	int tileType = tileIdentifier.second;
	if(index >= numTiles[tileType]) return 0;
	// Determine in which archive the tile with the specified index resides
	int archiveIndex = 0, // The index of the archive
		baseIndex = 0; // The absolute index of the first tile in our archive
	for(; archiveIndex < numArchives[tileType] && (baseIndex +
		graphicsFiles[tileType][archiveIndex].tileCount) <= index; ++archiveIndex)
		baseIndex += graphicsFiles[tileType][archiveIndex].tileCount;
	if(archiveIndex == numArchives[tileType]) return 0; // TODO: Better errors
	int localIndex = (index - baseIndex); // Determine the relative index of the tile
	// Open up the archive and read the header
	string fileName = (typeNames[tileType] + lexical_cast<string>(archiveIndex));
	ifstream in((dataPath + fileName + ".dat").c_str(), ios::binary);
	ArchiveHeader header;
	in >> header;
	ArchiveSeek(in, header, (fileName + ".epf").c_str());
	// Get the offset to the GraphicsTileInfo for our tile using the base info offset
	uint32 infoOffset = graphicsFiles[tileType][archiveIndex].infoOffset +
		12 /* Account for the header in our calculations */ + sizeof(GraphicsTileInfo) * localIndex;
	in.seekg(infoOffset, ios::cur); // Seek to the GraphicsTileInfo and read it in
	GraphicsTileInfo tileInfo;
	in.read((char *)&tileInfo, sizeof(GraphicsTileInfo));
	in.seekg(tileInfo.startOffset + 12 - infoOffset - sizeof(GraphicsTileInfo), ios::cur);
	uint8 *tileData = new uint8[tileInfo.GetWidth() * tileInfo.GetHeight() * 3];
	Palette &palette = paletteSets[tileType][paletteTables[tileType][index]];
	for(int y = 0; y < tileInfo.GetHeight(); ++y) {
		for(int x = 0; x < tileInfo.GetWidth(); ++x) {
			uint8 buffer;
			in.read((char *)&buffer, 1);
			uint32 dataOffset = (x + y * tileInfo.GetWidth()) * 3;
			wxColor &color = palette.data[buffer];
			tileData[dataOffset + 0] = color.Red();
			tileData[dataOffset + 1] = color.Green();
			tileData[dataOffset + 2] = color.Blue();
		}
	}
	// NOTE: This assumes that wxGLContext::SetCurrent is a threadsafe operation...?
	mainContext->SetCurrent();
	glEnable(GL_TEXTURE_2D);
	GLuint texture = 0;
	glGenTextures(1,  &texture);
	glBindTexture(GL_TEXTURE_2D, texture);
	gluBuild2DMipmaps(GL_TEXTURE_2D, 3, tileInfo.GetWidth(), tileInfo.GetHeight(),
		GL_RGB, GL_UNSIGNED_BYTE, tileData);
	glBindTexture(GL_TEXTURE_2D, 0);
	delete[] tileData;
	width = tileInfo.GetWidth();
	height = tileInfo.GetHeight();
	return texture;
}
void TileLoader::Init() {
	dataPath = "C:/program files/nexustk/data/"; // TEMP
	// TODO: Make sure that every archive has the necessary files
	ArchiveHeader header;
	{
		// Open up the main archive, tile.dat
		ifstream in((format("%1%tile.dat") % dataPath).str().c_str(), ios::binary);
		in >> header;
		for(int i = 0; i < 2; ++i) {
			string fileName(typeNames[i]);
			ArchiveSeek(in, header, (fileName + ".pal").c_str());
			in >> paletteSets[i]; // Read in palette information
			ArchiveSeek(in, header, (fileName + ".tbl").c_str());
			in >> paletteTables[i]; // Read in table information
		}
	}
	// Open up all of the graphics archives and determine how many tiles they contain
	for(int i = 0; i < 2; ++i) {
		for(int j = 0; j < numArchives[i]; ++j) {
			string fileName = (format("%1%%2%") % typeNames[i] % j).str();
			ifstream in((dataPath + fileName + ".dat").c_str(), ios::binary);
			in >> header;
			ArchiveSeek(in, header, (fileName + ".epf").c_str());
			GraphicsHeader graphicsHeader;
			in.read((char *)&graphicsHeader, sizeof(GraphicsHeader));
			graphicsFiles[i].push_back(graphicsHeader);
			numTiles[i] += graphicsHeader.tileCount;
		}
	}
}
istream &operator >>(istream &in, TileLoader::PaletteTable &table) {
	uint16 count;
	in.read((char *)&count, 2);
	in.seekg(2, ios::cur);
	table.reserve(count);
	uint8 buffer;
	for(int i = 0; i < count; ++i) {
		in.read((char *)&buffer, 1);
		in.seekg(1, ios::cur);
		table.push_back(buffer);
	}
	return in;
}
istream &operator >>(istream &in, TileLoader::PaletteSet &set) {
	uint8 count, type;
	in.read((char *)&count, 1);
	set.reserve(count);
	in.seekg(3, ios::cur);
	char header[10] = { 0 };
	int index = 0;
	while(index < count) {
		in.read((char *)&header, 9);
		if(strcmp(header, "DLPalette")) { /* TODO: Better errors */
			string error = (format("Bad header in palette %1%") % index).str();
			int32 tell = in.tellg(); // TEMP: What is this?
			throw exception(error.c_str());
		}
		in.seekg(15, ios::cur);
		in.read((char *)&type, 1);
		switch(type) {
			case 3: in.seekg(13, ios::cur); break;
			case 1: in.seekg(9, ios::cur); break;
			case 2: in.seekg(11, ios::cur); break;
			case 4: in.seekg(15, ios::cur); break;
			default: in.seekg(7, ios::cur); break;
		}
		uint32 buffer;
		set.push_back(TileLoader::Palette());
		for(int i = 0; i < 256; ++i) {
			in.read((char *)&buffer, 4);
			set[index].data[i] = wxColor(
				(buffer & 0x000000FF),
				(buffer & 0x0000FF00) >> 8,
				(buffer & 0x00FF0000) >> 16
			);
		}
		++index;
	}
	return in;
}
istream &operator >>(istream &in, TileLoader::ArchiveHeader &header) {
	header.files.clear();
	uint32 count;
	in.read((char *)&count, 4);
	for(int i = 0; i < (count - 1); ++i) {
		TileLoader::ArchiveFile file;
		in.read((char *)&file.offset, 4);
		in.read((char *)file.name, 13);
		transform(file.name, file.name + strlen(file.name), file.name, tolower);
		header.files.push_back(file);
	}
	return in;
}
uint32 TileLoader::ArchiveFileSize(
	istream &in, TileLoader::ArchiveHeader &header, const char *fileName) { return -1; }
void TileLoader::ArchiveSeek(
	istream &in, TileLoader::ArchiveHeader &header, const char *fileName) {
	vector<ArchiveFile>::iterator i = find(header.files.begin(), header.files.end(), fileName);
	if(i != header.files.end()) in.seekg(i->offset, ios::beg); else
		/* TODO: Better error handling */ throw exception("Invalid file requested");
}
bool TileLoader::ArchiveFile::operator ==(const char *compare) {
	string compareLower(compare);
	transform(compareLower.begin(), compareLower.end(), compareLower.begin(), tolower);
	return (compareLower == name);
}