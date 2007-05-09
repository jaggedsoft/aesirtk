# gen = lambda name: [ "%s%i" % (name, i) for i in range(1, 9) ]
# fields = gen("drop") + gen("dropRate") + gen("dropCount")
# fields = [ "StartMap", "StartX", "StartY", "Id" ]
# order = 1
dataTypeInit = { "int": "0", "string": "\"\"" }
def generate(fields, order = 0):
	for field in fields:
		dataType = 'int'
		publicName = None
		if type(field) == tuple:
			dataType = field[0]
			publicName = field[1]
		else: publicName = field
		if publicName[0].islower(): publicName = publicName[0].upper() + publicName[1:]
		privateName = publicName[0].lower() + publicName[1:]
		if privateName == 'class': privateName = 'classNumber'
		print "\t\tprivate %s %s = %s;" % (dataType, privateName, dataTypeInit[dataType])
		print "\t\t[PropertyOrder(%i)]" % order
		print "\t\tpublic %s %s {" % (dataType, publicName)
		print "\t\t\tget { return %s; }" % privateName
		print "\t\t\tset { %s = value; }" % privateName
		print "\t\t}"
		order = order + 1
dropHelper = lambda name: [ "%s%i" % (name, i) for i in range(1, 9) ]
dataTypeInit["TypeEnum"] = "TypeEnum.Use"
dataTypeInit["SexEnum"] = "SexEnum.All"
itemFields = [
	"number", ("string", "name"), ("string", "shortName"), "rank", ("TypeEnum", "type"),
	"price", "sell", "maxAmount", "class", ("SexEnum", "sex"), "level", "look", "lookColor",
	"icon", "iconColor", "sound", "durability", "might", "will", "grace", "armor", "hit",
	"damage", "vita", "mana", "protection", "healing", "minDamage", "maxDamage"
]
generate(itemFields)
