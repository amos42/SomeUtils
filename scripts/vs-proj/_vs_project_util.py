import sys
import os
import re
import xml.etree.ElementTree as etree
import _vs_version_util as vsver


class ProjectRefInfo:
    #id = None
    #version = None
    #newVersion = None
    def __init__(self, id, version):
        self.id = id
        self.projectPath = None
        self.version = version
        self.newVersion = None        

class ProjectFileInfo:
    #projectInfo = None #ProjectVerInfo(None, None)
    #assemblyVersion = None
    #assemblyFileVersion = None
    #refPkgs = None # list()
    #refPrjs = None # list()
    #frameworkinfo = None
    #projectPath = None
    #asmInfoPath = None
    def __init__(self):
        self.projRefInfo = ProjectRefInfo(None, None)
        self.assemblyVersion = None
        self.assemblyFileVersion = None
        self.refPkgs = list()
        self.refPrjs = list()
        self.frameworkinfo = None
        self.asmInfoPath = None
        self.isTestProject = False


def getProjectInfo(projectFilename): # return ProjectFileInfo
    projInfo = ProjectFileInfo()
    projInfo.isTestProject = projectFilename.endswith(".Test.csproj") or projectFilename.endswith(".Tests.csproj")

    print("Analisys ", projectFilename, " ...")

    projectFilename = os.path.abspath(projectFilename)
    projInfo.projRefInfo.projectPath = projectFilename

    projpath = os.path.dirname(projectFilename)

    doc = etree.parse(projectFilename)
    root = doc.getroot()    
    token = re.findall(r"^\{\s*(.*)\s*\}", root.tag)
    if token: 
        ns = {"": token[0]}
        etree.register_namespace("", token[0])
    else:
        ns = None

    version = None
    assemblyName = None
    assemVersion = None 
    assemFileVersion = None

    assemblyNode = root.find("PropertyGroup/AssemblyName", ns)
    if assemblyNode != None: assemblyName = assemblyNode.text
    assemblyName = os.path.splitext(os.path.basename(projectFilename))[0]
    versionNode = root.find("PropertyGroup/Version", ns)
    if versionNode != None: version = versionNode.text
    else: version = "1.0.0"

    framework = None
    t = root.find("PropertyGroup/TargetFramework", ns)
    if t != None:
        framework = t.text
        assemVersionNode = root.find("PropertyGroup/AssemblyVersion")
        if assemVersionNode != None: assemblyVersion = assemVersionNode.text
        else: assemblyVersion = "1.0.0.0"
        assemFileVersionNode = root.find("PropertyGroup/FileVersion")
        if assemFileVersionNode != None: assemblyFileVersion = assemFileVersionNode.text
        else: assemblyFileVersion = version
        projInfo.projRefInfo.id = assemblyName
        projInfo.projRefInfo.version = version
        projInfo.assemblyVersion = assemblyVersion
        projInfo.assemblyFileVersion = assemblyFileVersion
    else:
        t = root.find("PropertyGroup/TargetFrameworkVersion", ns)
        if t != None:
            assemblyVersion = None
            assemblyFileVersion = None
            framework = "netframework" + t.text
            #assembly = doc.xpath("foo:ItemGroup/foo:Compile[contains(@Include,'AssemblyInfo.cs')]", namespaces=dic_ns)
            assemblyInfoFileName = None
            compiles = root.findall("ItemGroup/Compile", ns)
            for compileNode in compiles:
                incAttr = compileNode.attrib["Include"]
                if (incAttr != None) and incAttr.endswith("AssemblyInfo.cs"):
                    assemblyInfoFileName = incAttr
                    break
            if assemblyInfoFileName != None:
                projInfo.asmInfoPath = os.path.abspath(os.path.join(projpath, assemblyInfoFileName))
                try:
                    f = open(projInfo.asmInfoPath, "r", encoding="utf-8")
                    #[assembly: AssemblyVersion("1.0.2.0")]
                    #[assembly: AssemblyFileVersion("1.0.2.0")]                
                    for line in f:
                        if re.match(r"\s*\[assembly:\s*AssemblyVersion\(\s*\".*\s*\"\)\]", line):
                            token = re.findall(r"^\(\"\s*(.*)\s*\"\)", line)
                            if token: assemblyVersion = token[0]
                        elif re.match(r"\s*\[assembly:\s*AssemblyFileVersion\(\s*\".*\s*\"\)\]", line):
                            token = re.findall(r"^\(\"\s*(.*)\s*\"\)", line)
                            if token: assemblyFileVersion = token[0]
                except PermissionError:
                    print("error")
                    pass
                projInfo.projRefInfo.id = assemblyName
                projInfo.projRefInfo.version = version
                projInfo.assemblyVersion = assemblyVersion
                projInfo.assemblyFileVersion = assemblyFileVersion
            else:
                print("> Not found assembly")

    projInfo.frameworkinfo = framework

    refpkgs = root.findall("ItemGroup/PackageReference", ns)
    for pkg in refpkgs: 
        refver = None
        if 'Version' in pkg.attrib:
            refver = pkg.attrib['Version']
        else:
            rv = pkg.find('Version', ns)
            if rv != None:
                refver = rv.text
        projInfo.refPkgs.append(ProjectRefInfo(pkg.attrib['Include'], refver))

    refprojs = root.findall("ItemGroup/ProjectReference", ns)
    for proj in refprojs:
        projInfo.refPrjs.append(os.path.abspath(os.path.join(projpath, proj.attrib['Include'])))

    return projInfo
    

def updateProjectInfo(projInfo, projDict, assemblyChangePackageList):
    projpath = os.path.dirname(projInfo.projRefInfo.projectPath)

    doc = etree.parse(projInfo.projRefInfo.projectPath)
    root = doc.getroot()    
    token = re.findall(r"^\{\s*(.*)\s*\}", root.tag)
    if token: 
        ns = {"": token[0]}
        etree.register_namespace("", token[0])
    else:
        ns = None

    isChange = False
    if not projInfo.isTestProject:
        if projInfo.asmInfoPath == None:
            firstPropertiesNode = root.find("PropertyGroup")
            if firstPropertiesNode == None:
                firstPropertiesNode = etree.SubElement(root, "PropertyGroup")
                firstPropertiesNode.tail = "\n"
            if projInfo.projRefInfo.id in assemblyChangePackageList:
                firstPropertiesNode.find("AssemblyVersion").text = vsver.convAssemblyVersion(projInfo.projRefInfo.newVersion)
            versionNode = firstPropertiesNode.find("Version")
            if versionNode == None:
                versionNode = etree.SubElement(firstPropertiesNode, "Version")
                versionNode.tail = "\n"
            versionNode.text = vsver.convProjectVersion(projInfo.projRefInfo.newVersion)
            fileVersionNode = firstPropertiesNode.find("FileVersion")
            if fileVersionNode == None:
                fileVersionNode = etree.SubElement(firstPropertiesNode, "FileVersion")
                fileVersionNode.tail = "\n"
            fileVersionNode.text = vsver.convAssemblyVersion(projInfo.projRefInfo.newVersion)
            isChange = True
        else:
            try:
                f = open(projInfo.asmInfoPath, "r", encoding="utf-8")
                #[assembly: AssemblyVersion("1.0.2.0")]
                #[assembly: AssemblyFileVersion("1.0.2.0")]    
                allline = list()
                for line in f:
                    if (assemblyChangePackageList == None) or (projInfo.projRefInfo.id in assemblyChangePackageList):
                        #root.find("PropertyGroup/AssemblyVersion").text = vsver.convAssemblyVersion(projInfo.projRefInfo.newVersion)
                        if re.match(r"\s*\[assembly:\s*AssemblyVersion\(\s*\".*\s*\"\)\]", line):
                            line = "[assembly: AssemblyVersion(\"" + vsver.convAssemblyVersion(projInfo.projRefInfo.newVersion) + "\")]\n"
                    elif re.match(r"\s*\[assembly:\s*AssemblyFileVersion\(\s*\".*\s*\"\)\]", line):
                        line = "[assembly: AssemblyFileVersion(\"" + vsver.convAssemblyVersion(projInfo.projRefInfo.newVersion) + "\")]\n"
                    allline.append(line)
                f.close()
                f = open(projInfo.asmInfoPath, "w", encoding="utf-8")
                f.write("".join(allline))
                f.close()
            except PermissionError:
                print("error")
                pass

    refpkgs = root.findall("ItemGroup/PackageReference", ns)
    for pkg in refpkgs: 
        refver = None
        pkgName = pkg.attrib['Include']
        for refInfo in projInfo.refPkgs:
            if refInfo.id == pkgName:
                if vsver.VersionCompare(refInfo.newVersion, refInfo.version) > 0:
                    if 'Version' in pkg.attrib:
                        pkg.attrib['Version'] = refInfo.newVersion
                    else:
                        rv = pkg.find('Version', ns)
                        if rv != None:
                            rv.text = refInfo.newVersion
                    isChange = True
                break
    
    if isChange:
        #doc.write(projInfo.projRefInfo.projectPath, encoding="utf-8", pretty_print=True, xml_declaration=True)
        #doc.write(projInfo.projRefInfo.projectPath, encoding="utf-8", pretty_print=True, doctype='<?xml version="1.0" encoding="utf-8"?>')
        doc.write(projInfo.projRefInfo.projectPath, encoding="utf-8", xml_declaration=True)

    nuspecPath = os.path.join(projpath, "Module.nuspec")
    updateNugetRefs(projInfo, nuspecPath, projDict)


def updateNugetRefs(projInfo, nuspecPath, projDict):
    if not os.path.exists(nuspecPath): return

    doc = etree.parse(nuspecPath)
    root = doc.getroot()

    #root.find("metadata/version").text = vsver.convProjectVersion(projInfo.projRefInfo.newVersion)
    root.find("metadata/version").text = projInfo.projRefInfo.newVersion

    deps = root.findall("metadata/dependencies/dependency")
    deps2 = root.findall("metadata/dependencies/group/dependency")
    deps.extend(deps2)
    for dep in deps:
        id = dep.attrib['id']
        isfind = False
        for refInfo in projInfo.refPkgs:
            if refInfo.id == id:
                if vsver.VersionCompare(refInfo.newVersion, refInfo.version) > 0:
                    dep.attrib['version'] = refInfo.newVersion
                isfind = True
                break
        if isfind: continue
        for refpath in projInfo.refPrjs:
            refInfo = projDict[refpath].projRefInfo
            if refInfo.id == id:
                if vsver.VersionCompare(refInfo.newVersion, refInfo.version) > 0:
                    dep.attrib['version'] = refInfo.newVersion
                break

    #doc.write(nuspecPath, encoding="utf-8", pretty_print=True, doctype='<?xml version="1.0" encoding="utf-8"?>')
    doc.write(nuspecPath, encoding="utf-8", xml_declaration=True)
