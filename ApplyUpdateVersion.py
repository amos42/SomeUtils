import sys
import os
import re
import shutil
from lxml import etree
import inspect

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
    #refPkgs = None # list()
    #refPrjs = None # list()
    #frameworkinfo = None
    #projectPath = None
    #asmInfoPath = None
    def __init__(self):
        self.projRefInfo = ProjectRefInfo(None, None)
        self.refPkgs = list()
        self.refPrjs = list()
        self.frameworkinfo = None
        self.asmInfoPath = None
        self.isTestProject = False

projectDict = dict()


def getProjectInfo(projectFilename):
    projInfo = ProjectFileInfo()
    projInfo.isTestProject = projectFilename.endswith(".Test.csproj") or projectFilename.endswith(".Tests.csproj")

    projectFilename = os.path.abspath(projectFilename)
    projInfo.projRefInfo.projectPath = projectFilename

    projpath = os.path.dirname(projectFilename)

    doc = etree.parse(projectFilename)
    root = doc.getroot()    
    dic_ns = {}
    for element in root.nsmap:
       if element is None: 
         dic_ns["foo"] = root.nsmap[None]
       else:
         dic_ns[element] = root.nsmap[element]

    assemblyName = root.find("PropertyGroup/AssemblyName", root.nsmap).text
    framework = None
    t = root.find("PropertyGroup/TargetFramework", root.nsmap)
    if t != None:
        framework = t.text
        version = root.find("PropertyGroup/Version").text
        assemVersion = root.find("PropertyGroup/AssemblyVersion").text
        assemFileVersion = root.find("PropertyGroup/FileVersion").text
        projInfo.projRefInfo.id = assemblyName
        projInfo.projRefInfo.version = assemVersion
    else:
        t = root.find("PropertyGroup/TargetFrameworkVersion", root.nsmap)
        if t != None:
            framework = "netframework" + t.text
            assembly = doc.xpath("foo:ItemGroup/foo:Compile[contains(@Include,'AssemblyInfo.cs')]", namespaces=dic_ns)
            if assembly != None and len(assembly) > 0:
                assemFileName = assembly[0].attrib["Include"]
                assemFileName = os.path.abspath(os.path.join(projpath, assemFileName))
                projInfo.asmInfoPath = assemFileName
                try:
                    f = open(assemFileName, "r", encoding="utf-8")
                    #[assembly: AssemblyVersion("1.0.2.0")]
                    #[assembly: AssemblyFileVersion("1.0.2.0")]                
                    for line in f:
                        if re.match(r"\s*\[assembly:\s*AssemblyVersion\(\s*\".*\s*\"\)\]", line):
                            token = re.findall(r"\(\"\s*(.*)\s*\"\)", line)
                            if len(token) >= 1: 
                                assemVersion = token[0]
                        elif re.match(r"\s*\[assembly:\s*AssemblyFileVersion\(\s*\".*\s*\"\)\]", line):
                            token = re.findall(r"\(\"\s*(.*)\s*\"\)", line)
                            if len(token) >= 1: 
                                assemFileVersion = token[0]
                    projInfo.projRefInfo.id = assemblyName
                    projInfo.projRefInfo.version = assemVersion
                except PermissionError:
                    print("error")
                    pass
            else:
                print("> Not found assembly")

    projInfo.frameworkinfo = framework

    refpkgs = root.findall("ItemGroup/PackageReference", root.nsmap)
    for pkg in refpkgs: 
        refver = None
        if 'Version' in pkg.attrib:
            refver = pkg.attrib['Version']
        else:
            rv = pkg.find('Version', root.nsmap)
            if rv != None:
                refver = rv.text
        projInfo.refPkgs.append(ProjectRefInfo(pkg.attrib['Include'], refver))

    refprojs = root.findall("ItemGroup/ProjectReference", root.nsmap)
    for proj in refprojs:
        projInfo.refPrjs.append(os.path.abspath(os.path.join(projpath, proj.attrib['Include'])))

    return projInfo
    

def applyChangeProject(projInfo, projDict):
    projpath = os.path.dirname(projInfo.projRefInfo.projectPath)

    doc = etree.parse(projInfo.projRefInfo.projectPath)
    root = doc.getroot()    
    dic_ns = {}
    for element in root.nsmap:
       if element is None: 
         dic_ns["foo"] = root.nsmap[None]
       else:
         dic_ns[element] = root.nsmap[element]

    framework = None
    if not projInfo.isTestProject:
        if projInfo.asmInfoPath == None:
            root.find("PropertyGroup/Version").text = convProjectVersion(projInfo.projRefInfo.newVersion)
            root.find("PropertyGroup/AssemblyVersion").text = convAssemblyVersion(projInfo.projRefInfo.newVersion)
            root.find("PropertyGroup/FileVersion").text = convAssemblyVersion(projInfo.projRefInfo.newVersion)
        else:
            try:
                f = open(projInfo.asmInfoPath, "r", encoding="utf-8")
                #[assembly: AssemblyVersion("1.0.2.0")]
                #[assembly: AssemblyFileVersion("1.0.2.0")]    
                allline = list()
                for line in f:
                    if re.match(r"\s*\[assembly:\s*AssemblyVersion\(\s*\".*\s*\"\)\]", line):
                        line = "[assembly: AssemblyVersion(\"" + convAssemblyVersion(projInfo.projRefInfo.newVersion) + "\")]\n"
                    elif re.match(r"\s*\[assembly:\s*AssemblyFileVersion\(\s*\".*\s*\"\)\]", line):
                        line = "[assembly: AssemblyFileVersion(\"" + convAssemblyVersion(projInfo.projRefInfo.newVersion) + "\")]\n"
                    allline.append(line)
                f.close()
                f = open(projInfo.asmInfoPath, "w", encoding="utf-8")
                f.write("".join(allline))
                f.close()
            except PermissionError:
                print("error")
                pass

    refpkgs = root.findall("ItemGroup/PackageReference", root.nsmap)
    for pkg in refpkgs: 
        refver = None
        pkgName = pkg.attrib['Include']
        for refInfo in projInfo.refPkgs:
            if refInfo.id == pkgName:
                if VersionCompare(refInfo.newVersion, refInfo.version) > 0:
                    if 'Version' in pkg.attrib:
                        pkg.attrib['Version'] = refInfo.newVersion
                    else:
                        rv = pkg.find('Version', root.nsmap)
                        if rv != None:
                            rv.text = refInfo.newVersion
                break
    
    #doc.write(projInfo.projRefInfo.projectPath, encoding="utf-8", pretty_print=True, xml_declaration=True)
    doc.write(projInfo.projRefInfo.projectPath, encoding="utf-8", doctype='<?xml version="1.0" encoding="utf-8"?>')

    nuspecPath = os.path.join(projpath, "Module.nuspec")
    if os.path.exists(nuspecPath):
        doc = etree.parse(nuspecPath)
        root = doc.getroot()

        root.find("metadata/version").text = convProjectVersion(projInfo.projRefInfo.newVersion)

        deps = root.findall("metadata/dependencies/dependency")
        deps2 = root.findall("metadata/dependencies/group/dependency")
        deps.extend(deps2)
        for dep in deps:
            id = dep.attrib['id']
            isfind = False
            for refInfo in projInfo.refPkgs:
                if refInfo.id == id:
                    if VersionCompare(refInfo.newVersion, refInfo.version) > 0:
                        dep.attrib['version'] = refInfo.newVersion
                    isfind = True
                    break
            if isfind: continue
            for refpath in projInfo.refPrjs:
                refInfo = projDict[refpath].projRefInfo
                if refInfo.id == id:
                    if VersionCompare(refInfo.newVersion, refInfo.version) > 0:
                        dep.attrib['version'] = refInfo.newVersion
                    isfind = True
                    break
        doc.write(nuspecPath, encoding="utf-8", doctype='<?xml version="1.0" encoding="utf-8"?>')


def getProject(projLst, moduleName):
    for proj in projLst:
        if proj.projRefInfo.id == moduleName: 
            return proj
    return None


def findProjectByPath(projLst, path):
    for proj in projLst:
        if proj.projRefInfo.projectPath == path: 
            return proj
    return None


def getProjectInfoList(solutionFilename):
    #solutionFilename = os.path.abspath(solutionFilename)
    projList = list()
    try:
        f = open(solutionFilename, "r", encoding="utf-8")
        for line in f:
            if re.match(r"\s*Project\s*\(\s*\"\{.*\}\"\s*\)", line):
                token = re.findall(r"=\s*\"([^\"]*)\"\s*,\s*\"([^\"]*)\"", line)
                if len(token) >= 1: 
                    projname = token[0][0]
                    filename = token[0][1]
                    if filename.endswith(".csproj"):
                        slnPath = os.path.dirname(solutionFilename)
                        filename = os.path.join(slnPath, filename)
                        projList.append(getProjectInfo(filename))
                #print(token)
    except PermissionError:
        print("error")
        return null

    return projList


def setProjectDict(projList):
    for proj in projList:
        projectDict[proj.projRefInfo.id] = proj
        projectDict[proj.projRefInfo.projectPath] = proj


def addVersion(version, inc):
    vers = list(map(int, version.split(".")))
    incs = list(map(int, inc.split(".")))
    l1 = len(vers)
    l2 = len(incs)
    newVersion = ""
    for i in range(0, l1):
        if i < l2: vers[i] += incs[i]
        if newVersion != "": newVersion = newVersion + "."
        newVersion = newVersion + str(vers[i])
    return newVersion


def VersionCompare(version, baseVersion):
    if version == None: return -1
    if baseVersion == None: return 1
    vers1 = version.split(".")
    vers2 = baseVersion.split(".")
    l1 = len(vers1)
    l2 = len(vers2)
    l3 = l1
    if l2 > l3: l3 = l2
    for i in range(0, l3):
        if i < l1: v1 = int(vers1[i])
        else: v1 = 0        
        if i < l2: v2 = int(vers2[i]) 
        else: v2 = 0

        if v1 > v2: return 1
        elif v1 < v2: return -1
    return 0


def convProjectVersion(version):
    if version == None: return "0.0.0"
    vers = version.split(".")
    l = len(vers)
    if len == 3: return version
    value = ""
    for i in range(0, 3):
        if i < 3: v = vers[i]
        else: v = "0"
        if value != "": value = value + "."
        value = value + v
    return value


def convAssemblyVersion(version):
    if version == None: return "0.0.0.0"
    vers = version.split(".")
    l = len(vers)
    if len == 4: return version
    value = ""
    for i in range(0, 4):
        if i < 4: v = vers[i]
        else: v = "0"
        if value != "": value = value + "."
        value = value + v
    return value


def setNewVersion(refInfo, newVersion):
    if newVersion == None: return False
    curVersion = refInfo.newVersion
    if curVersion == None: curVersion = refInfo.version
    if VersionCompare(newVersion, curVersion) > 0:
        refInfo.newVersion = newVersion
        return True
    else:
        return False


def setProjectNewVersion(project, newVersion, changeList):
    result = setNewVersion(project.projRefInfo, newVersion)
    if result and changeList != None:
        changeList.append(project)
    return result


def setProjectNewVersionByName(projDict, moduleName, newVersion, changeList):
    proj = projDict.get(moduleName)
    if proj == None: return False
    return setProjectNewVersion(proj, newVersion, changeList)


def analizeProjectList(projList, projDict, changeList):
    if len(changeList) <= 0: return

    while True:
        chageProjCnt = 0
        for proj in projList:
            if proj in changeList: continue

            chagenCnt = 0
            for ref in proj.refPkgs:
                for prj2 in changeList:
                    if ref.id == prj2.projRefInfo.id:
                        if setNewVersion(ref, prj2.projRefInfo.newVersion): chagenCnt = chagenCnt + 1
                        break
            for ref in proj.refPrjs:
                proj2 = projDict[ref]
                if proj2 in changeList: chagenCnt = chagenCnt + 1

            if chagenCnt > 0:
                setProjectNewVersion(proj, addVersion(proj.projRefInfo.version, "0.0.1"), changeList)
                chageProjCnt = chageProjCnt + 1

        if chageProjCnt <= 0: break


def applyChangeProjectList(projList, projDict):
    for proj in changelist:
        if proj.projRefInfo.projectPath == None: continue
        applyChangeProject(proj, projDict)


if __name__ == '__main__':
    if len(sys.argv) < (3+1):
        print("ApplyUpdateVersion.py <Solution File> <Module Name> <New Version>")
        exit(1)

    solutionFilename = sys.argv[1]
    moduleName = sys.argv[2]
    newVersion = sys.argv[3]

    projList = getProjectInfoList(solutionFilename)

    setProjectDict(projList)

    changelist = list()
    result = setProjectNewVersionByName(projectDict, moduleName, newVersion, changelist)
    if not result:
        proj = ProjectFileInfo()
        proj.projRefInfo.id = moduleName
        proj.projRefInfo.newVersion = newVersion
        changelist.append(proj)
    
    analizeProjectList(projList, projectDict, changelist)
        
    print("---------------------------")
    for projInfo in changelist:
        if projInfo.projRefInfo.newVersion == None: continue
        print("* project info :", projInfo.projRefInfo.id, projInfo.projRefInfo.version, " => ", projInfo.projRefInfo.newVersion)
        print("  * framework info :", projInfo.frameworkinfo)
        print("  * project path :", projInfo.projRefInfo.projectPath)
        print("  * assembly info path :", projInfo.asmInfoPath)
        print("  * ref packages:")
        for proj in projInfo.refPkgs:
            print("   > ", proj.id, proj.version, " => ", proj.newVersion)
        print("  * ref projects:")
        for proj0 in projInfo.refPrjs:
            proj = projectDict[proj0].projRefInfo
            print("   > ", proj.id, proj.version, " => ", proj.newVersion)
        print("  * test project :", projInfo.isTestProject)
        print("---------------------------")

    applyChangeProjectList(projList, projectDict)

    print("All Done.")
