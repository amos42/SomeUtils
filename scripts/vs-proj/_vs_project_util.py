import sys
import os
import re
import json
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

    def toString(self):
        if self.id != None: i = self.id
        else: i = "None"
        if self.version != None: v = self.version.toString()
        else: v = "None"
        if self.newVersion != None: 
            return i + " (" + v + " => " + self.newVersion.toString() + ")"
        else:
            return i + " (" + v + ")"
        

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
        self.packageId = None
        self.assemblyVersion = None
        self.assemblyFileVersion = None
        self.refPkgs = list()
        self.refPrjs = list()
        self.frameworkinfo = None
        self.asmInfoPath = None
        self.isTestProject = False


def findRefInfo(refInfoList, refId):
    if not refInfoList: return None
    for refInfo in refInfoList:
        if refInfo.id == refId: return refInfo
    return None

def getProjectInfo(projectFilename): # return ProjectFileInfo
    projInfo = ProjectFileInfo()
    projInfo.isTestProject = projectFilename.endswith(".Test.csproj") or projectFilename.endswith(".Tests.csproj")

    print("Analisys", projectFilename, "...")

    projectFilename = os.path.abspath(projectFilename)
    projpath = os.path.dirname(projectFilename)

    projInfo.projRefInfo.projectPath = projectFilename

    projpath = os.path.dirname(projectFilename)

    doc = etree.parse(projectFilename)
    root = doc.getroot()    
    token = re.findall(r"^\{\s*(.*)\s*\}", root.tag)
    if token: 
        ns = {"vsproj": token[0]}
        etree.register_namespace("", token[0])
    else:
        ns = None

    version = None
    assemblyName = None
    assemVersion = None 
    assemFileVersion = None

    if ns != None:
        assemblyNode = root.find("vsproj:PropertyGroup/vsproj:AssemblyName", ns)
    else:
        assemblyNode = root.find("PropertyGroup/AssemblyName")
    if assemblyNode != None: assemblyName = assemblyNode.text
    else: assemblyName = os.path.splitext(os.path.basename(projectFilename))[0]
    if ns != None:
        versionNode = root.find("vsproj:PropertyGroup/vsproj:Version", ns)
    else:
        versionNode = root.find("PropertyGroup/Version")
    if versionNode != None: version = vsver.SemVersion(versionNode.text)
    else: version = vsver.SemVersion("1.0.0")

    framework = None
    if ns != None:
        t = root.find("vsproj:PropertyGroup/vsproj:TargetFramework", ns)
    else:
        t = root.find("PropertyGroup/TargetFramework")
    if t != None:
        framework = t.text
        assemVersionNode = root.find("PropertyGroup/AssemblyVersion")
        if assemVersionNode != None: assemblyVersion = vsver.SemVersion(assemVersionNode.text)
        else: assemblyVersion = vsver.SemVersion("1.0.0.0")
        assemFileVersionNode = root.find("PropertyGroup/FileVersion")
        if assemFileVersionNode != None: assemblyFileVersion = vsver.SemVersion(assemFileVersionNode.text)
        else: assemblyFileVersion = version
        projInfo.projRefInfo.id = assemblyName
        projInfo.projRefInfo.version = version
        projInfo.assemblyVersion = assemblyVersion
        projInfo.assemblyFileVersion = assemblyFileVersion
    else:
        if ns != None:
            t = root.find("vsproj:PropertyGroup/vsproj:TargetFrameworkVersion", ns)
        else:
            t = root.find("PropertyGroup/TargetFrameworkVersion")
        if t != None:
            assemblyVersion = None
            assemblyFileVersion = None
            framework = "netframework" + t.text
            #assembly = doc.xpath("foo:ItemGroup/foo:Compile[contains(@Include,'AssemblyInfo.cs')]", namespaces=dic_ns)
            assemblyInfoFileName = None
            if ns != None:
                compiles = root.findall("vsproj:ItemGroup/vsproj:Compile", ns)
            else:
                compiles = root.findall("ItemGroup/Compile")
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
                            token = re.findall(r"\(\"\s*(.*)\s*\"\)", line)
                            if token: assemblyVersion = vsver.SemVersion(token[0], 4)
                        elif re.match(r"\s*\[assembly:\s*AssemblyFileVersion\(\s*\".*\s*\"\)\]", line):
                            token = re.findall(r"\(\"\s*(.*)\s*\"\)", line)
                            if token: assemblyFileVersion = vsver.SemVersion(token[0], 4)
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

    if ns != None:
        refpkgs = root.findall("vsproj:ItemGroup/vsproj:PackageReference", ns)
    else:
        refpkgs = root.findall("ItemGroup/PackageReference")
    for pkg in refpkgs: 
        refver = None
        if 'Version' in pkg.attrib:
            refver = pkg.attrib['Version']
        else:
            if ns != None:
                rv = pkg.find('vsproj:Version', ns)
            else:
                rv = pkg.find('Version')
            if rv != None:
                refver = rv.text
        if not refver: refver = "1.0.0"
        projInfo.refPkgs.append(ProjectRefInfo(pkg.attrib['Include'], vsver.SemVersion(refver)))

    if ns != None:
        refprojs = root.findall("vsproj:ItemGroup/vsproj:ProjectReference", ns)
    else:
        refprojs = root.findall("ItemGroup/ProjectReference")
    for proj in refprojs:
        projInfo.refPrjs.append(os.path.abspath(os.path.join(projpath, proj.attrib['Include'])))

    nuspecPath = os.path.join(projpath, "Module.nuspec")
    if(os.path.exists(nuspecPath)):
        readNugetRefs(projInfo, nuspecPath)
    else:
        if ns != None:
            ispkgnode = root.find("vsproj:PropertyGroup/vsproj:GeneratePackageOnBuild", ns)
        else:
            ispkgnode = root.find("PropertyGroup/GeneratePackageOnBuild")
        if ispkgnode != None:
            ispkg = bool(ispkgnode.text)
        else:        
            pkgInfoPath = os.path.join(projpath, "Packageinfo.json")
            if(os.path.exists(pkgInfoPath)):
                readPackageInfo(projInfo, pkgInfoPath)

    return projInfo


def readNugetRefs(projInfo, nuspecPath):
    if not os.path.exists(nuspecPath): return

    doc = etree.parse(nuspecPath)
    root = doc.getroot()

    projInfo.projRefInfo.id = root.find("metadata/id").text
    projInfo.projRefInfo.version = vsver.SemVersion(root.find("metadata/version").text)
    projInfo.packageId = projInfo.projRefInfo.id


def readPackageInfo(projInfo, pkgjInfoPath):
    if not os.path.exists(pkgjInfoPath): return

    with open(pkgjInfoPath, 'r', encoding="utf-8") as f:
        pkgInfoData = json.load(f)

    projInfo.packageId = pkgInfoData.get("package.id")


def updateProjectInfo(projInfo, projDict, assemblyChangePackageList, assemblyFileChangePackageList, excludePackageList):
    projpath = os.path.dirname(projInfo.projRefInfo.projectPath)

    doc = etree.parse(projInfo.projRefInfo.projectPath)
    root = doc.getroot()    
    token = re.findall(r"^\{\s*(.*)\s*\}", root.tag)
    if token: 
        ns = {"vsproj": token[0]}
        etree.register_namespace("", token[0])
    else:
        ns = None

    isCsprojFileChange = False

    if not projInfo.isTestProject:
        if projInfo.projRefInfo.id in excludePackageList: return

        if projInfo.asmInfoPath == None:
            firstPropertiesNode = root.find("PropertyGroup")
            if firstPropertiesNode == None:
                firstPropertiesNode = etree.SubElement(root, "PropertyGroup")
                firstPropertiesNode.tail = "\n"

            versionNode = root.find("PropertyGroup/Version")
            if versionNode == None:
                versionNode = etree.SubElement(firstPropertiesNode, "Version")
                versionNode.tail = "\n"
            versionNode.text = projInfo.projRefInfo.newVersion.toString(3)

            if projInfo.projRefInfo.id in assemblyChangePackageList:
                asemVersionNode = root.find("PropertyGroup/AssemblyVersion")
                if asemVersionNode == None:
                    asemVersionNode = etree.SubElement(firstPropertiesNode, "AssemblyVersion")
                    asemVersionNode.tail = "\n"
                asemVersionNode.text = projInfo.projRefInfo.newVersion.toString(4, False)

            if projInfo.projRefInfo.id in assemblyFileChangePackageList:
                fileVersionNode = root.find("PropertyGroup/FileVersion")
                if fileVersionNode == None:
                    fileVersionNode = etree.SubElement(firstPropertiesNode, "FileVersion")
                    fileVersionNode.tail = "\n"
                fileVersionNode.text = projInfo.projRefInfo.newVersion.toString(4, False)

            isCsprojFileChange = True
        else:
            try:
                f = open(projInfo.asmInfoPath, "r", encoding="utf-8")
                #[assembly: AssemblyVersion("1.0.2.0")]
                #[assembly: AssemblyFileVersion("1.0.2.0")]    
                allline = list()
                for line in f:
                    if re.match(r"\s*\[assembly:\s*AssemblyVersion\(\s*\".*\s*\"\)\]", line):
                        if projInfo.projRefInfo.id in assemblyChangePackageList:
                            line = "[assembly: AssemblyVersion(\"" + projInfo.projRefInfo.newVersion.toString(4, False) + "\")]\n"
                    elif re.match(r"\s*\[assembly:\s*AssemblyFileVersion\(\s*\".*\s*\"\)\]", line):
                        if projInfo.projRefInfo.id in assemblyFileChangePackageList:
                            line = "[assembly: AssemblyFileVersion(\"" + projInfo.projRefInfo.newVersion.toString(4, False) + "\")]\n"
                    allline.append(line)
                f.close()
                f = open(projInfo.asmInfoPath, "w", encoding="utf-8")
                f.write("".join(allline))
                f.close()
            except PermissionError:
                print("error")
                pass

    if ns != None:
        refpkgs = root.findall("vsproj:ItemGroup/vsproj:PackageReference", ns)
    else:
        refpkgs = root.findall("ItemGroup/PackageReference")
    for pkg in refpkgs: 
        refver = None
        pkgName = pkg.attrib['Include']
        refInfo = findRefInfo(projInfo.refPkgs, pkgName)
        if refInfo == None: continue
        if vsver.VersionCompare(refInfo.newVersion, refInfo.version) > 0:
            if 'Version' in pkg.attrib:
                pkg.attrib['Version'] = refInfo.newVersion.toString(3)
            else:
                if ns != None:
                    rv = pkg.find("vsproj:Version", ns)
                else:
                    rv = pkg.find("Version")
                if rv != None:
                    rv.text = refInfo.newVersion.toString(3)
            isCsprojFileChange = True
    
    if isCsprojFileChange:
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
    root.find("metadata/version").text = projInfo.projRefInfo.newVersion.toString(3)

    deps = root.findall("metadata/dependencies/dependency")
    deps2 = root.findall("metadata/dependencies/group/dependency")
    deps.extend(deps2)
    for dep in deps:
        id = dep.attrib['id']
        isfind = False
        for refInfo in projInfo.refPkgs:
            if refInfo.id == id:
                if vsver.VersionCompare(refInfo.newVersion, refInfo.version) > 0:
                    dep.attrib['version'] = refInfo.newVersion.toString(3)
                isfind = True
                break
        if isfind: continue
        for refpath in projInfo.refPrjs:
            refInfo = projDict[refpath].projRefInfo
            if refInfo.id == id:
                if vsver.VersionCompare(refInfo.newVersion, refInfo.version) > 0:
                    dep.attrib['version'] = refInfo.newVersion.toString(3)
                break

    #doc.write(nuspecPath, encoding="utf-8", pretty_print=True, doctype='<?xml version="1.0" encoding="utf-8"?>')
    doc.write(nuspecPath, encoding="utf-8", xml_declaration=True)
