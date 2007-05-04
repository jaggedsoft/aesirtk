# gen = lambda name: [ "%s%i" % (name, i) for i in range(1, 9) ]
# fields = gen("drop") + gen("dropRate") + gen("dropCount")
# fields = [ "StartMap", "StartX", "StartY", "Id" ]
# order = 1
order = 10
fields = [ "might", "grace", "moveTime", "spawnTime" ]
for field in fields:
	if field[0].islower(): field = field[0].upper() + field[1:]
	publicName = field
	privateName = field[0].lower() + field[1:]
	print "\t\tprivate int %s = 0;" % privateName
	print "\t\t[PropertyOrder(%i)]" % order
	print "\t\t[Category(\"Stats\")]"
	print "\t\tpublic int %s {" % publicName
	print "\t\t\tget { return %s; }" % privateName
	print "\t\t\tset { %s = value; }" % privateName
	print "\t\t}"
	order = order + 1
