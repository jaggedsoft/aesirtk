using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CSharp;
using System.Xml;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.IO;

namespace DbProcessor {
	class Program {
		private const string targetNamespace = "DbTool";
		private static string JoinNames(params string[] names) {
			return string.Join(".", names);
		}
		private static CodeTypeDeclaration LoadFileElement(XmlElement element,
			CodeNamespace codeNamespace) {

			string className = element.GetAttribute("class");
			CodeTypeDeclaration codeType = new CodeTypeDeclaration(className);
			codeType.Attributes = MemberAttributes.Assembly;
			codeType.BaseTypes.Add(new CodeTypeReference("DbEntry"));
			int order = 0;
			foreach(XmlNode node in element.ChildNodes) {
				if(!(node is XmlElement)) continue;
				XmlElement childElement = (XmlElement)node;
				CodeMemberProperty codeProperty;
				if(childElement.Name == "field")
					LoadFieldElement(childElement, codeType, out codeProperty);
				else if(childElement.Name == "enum")
					LoadEnumElement(childElement, codeType, out codeProperty);
				else continue;
				codeProperty.CustomAttributes.Add(new CodeAttributeDeclaration(
					new CodeTypeReference("PropertyOrderAttribute"),
					new CodeAttributeArgument(new CodePrimitiveExpression(order++))));
			}
			CodeMemberMethod stringMethod = new CodeMemberMethod();
			stringMethod.Name = "ToString";
			stringMethod.Attributes = MemberAttributes.Public | MemberAttributes.Override;
			string stringMethodBody = element["string"].InnerText.TrimEnd(';');
			stringMethod.Statements.Add(new CodeSnippetExpression(stringMethodBody));
			stringMethod.ReturnType = new CodeTypeReference("System.String");
			codeType.Members.Add(stringMethod);
			return codeType;
		}
		private static void LoadEnumElement(XmlElement element, CodeTypeDeclaration codeType,
			out CodeMemberProperty codeProperty) {

			codeProperty = new CodeMemberProperty();
			string name = element.GetAttribute("name");
			CodeTypeDeclaration codeEnum = new CodeTypeDeclaration(ToPublicName(name) + "Enum");
			codeEnum.IsEnum = true;
			codeEnum.Members.Add(new CodeSnippetTypeMember(element.InnerText));
			codeType.Members.Add(codeEnum);
			CodeExpression defaultValue = null;
			if(element.HasAttribute("default"))
				defaultValue = new CodeSnippetExpression(
				JoinNames(codeEnum.Name, element.GetAttribute("default")));
			GenerateProperty(codeType, codeEnum.Name, name, out codeProperty, defaultValue);
		}
		private static string ToPrivateName(string name) {
			return name[0].ToString().ToLower() + name.Substring(1);
		}
		private static string ToPublicName(string name) {
			return name[0].ToString().ToUpper() + name.Substring(1);
		}
		private static void GenerateProperty(CodeTypeDeclaration codeType, string type, string name,
			out CodeMemberProperty codeProperty, CodeExpression defaultValue) {

			codeProperty = new CodeMemberProperty();
			CodeMemberField codeField = new CodeMemberField();
			codeField.Name = ToPrivateName(name);
			codeProperty.Name = ToPublicName(name);
			CodeTypeReference codeFieldType = new CodeTypeReference(type);
			codeProperty.Type = codeField.Type = codeFieldType;
			if(defaultValue != null) codeField.InitExpression = defaultValue;
			CodeFieldReferenceExpression codeFieldReference = new CodeFieldReferenceExpression(
				new CodeThisReferenceExpression(), codeField.Name);
			codeProperty.GetStatements.Add(new CodeMethodReturnStatement(codeFieldReference));
			codeProperty.SetStatements.Add(new CodeAssignStatement(codeFieldReference,
				new CodePropertySetValueReferenceExpression()));
			codeProperty.Attributes = MemberAttributes.Public | MemberAttributes.Final;
			codeField.Attributes = MemberAttributes.Private;
			codeType.Members.Add(codeProperty);
			codeType.Members.Add(codeField);
		}
		private static void LoadFieldElement(XmlElement element, CodeTypeDeclaration codeType,
			out CodeMemberProperty codeProperty) {

			XmlAttributeCollection attr = element.Attributes;
			string type = element.GetAttribute("type"), name = element.GetAttribute("name");
			if(type == "string") type = "System.String";
			else if(type == "int") type = "System.Int32";
			CodeExpression defaultValue = null;
			if(element.HasAttribute("default")) {
				string defaultString = element.GetAttribute("default");
				if(type == "System.String")
					defaultValue = new CodePrimitiveExpression(defaultString);
				if(type == "System.Int32")
					defaultValue = new CodePrimitiveExpression(int.Parse(defaultString));
			}
			GenerateProperty(codeType, type, name, out codeProperty, defaultValue);
		}
		static void Main(string[] args) {
			XmlDocument document = new XmlDocument();
			string basePath = @"C:\Code\Nexus\DbTool\";
			File.Delete(Path.Combine(basePath, "Db.cs"));
			document.Load(Path.Combine(basePath, "db.xml"));
			CodeNamespace codeNamespace = new CodeNamespace("DbTool");
			Dictionary<string, CodeTypeDeclaration> files = new Dictionary<string, CodeTypeDeclaration>();
			foreach(XmlNode node in document.FirstChild.ChildNodes) {
				if(!(node is XmlElement)) continue;
				XmlElement element = (XmlElement)node;
				if(element.Name == "file") {
					CodeTypeDeclaration codeType = LoadFileElement(element, codeNamespace);
					files.Add(element.GetAttribute("name"), codeType);
					codeNamespace.Types.Add(codeType);
				}
			}
			CodeTypeDeclaration entriesCodeType = new CodeTypeDeclaration("DbEntries");
			CodeMemberMethod initMethod = new CodeMemberMethod();
			initMethod.Name = "Init";
			initMethod.Attributes = MemberAttributes.Public | MemberAttributes.Static;
			foreach(string name in files.Keys) {
				CodeSnippetExpression factoryDelegate = new CodeSnippetExpression(
					string.Format("delegate() {{ return new {0}(); }}", files[name].Name));
				initMethod.Statements.Add(new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(
					new CodeTypeReferenceExpression("DbEntry"), "Register"), new CodePrimitiveExpression(name),
					factoryDelegate));
			}
			entriesCodeType.Members.Add(initMethod);
			codeNamespace.Types.Add(entriesCodeType);
			StreamWriter textWriter = new StreamWriter(new FileStream(Path.Combine(basePath, "Db.cs"),
				FileMode.OpenOrCreate));
			new CSharpCodeProvider().GenerateCodeFromNamespace(codeNamespace, textWriter,
				new CodeGeneratorOptions());
			textWriter.Flush();
		}
	}
}