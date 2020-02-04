import sys
import os
import re
import shutil
import argparse
import xml.etree.ElementTree as etree

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

projectDict = dict()


def getArguments():
    parser = argparse.ArgumentParser()
    parser.add_argument(dest='solutionFilename', help='Example) Sample.sln')
    parser.add_argument('--change-packages', '-c', nargs='+', default=[], metavar='"PackageName Version"', dest='changePackageList', help='Example) "DevPlatfomr.Base 1.0.1" "DevPlatfomr.DB 1.0.6"')
    parser.add_argument('--assembly-change-packages', '-a', nargs='*', default=[], metavar='"PackageName"', dest='assemblyChangePackageList', help='Example) DevPlatfomr.Base DevPlatfomr.DB DevPlatfomr.DB.*')

    solutionFilename = parser.parse_args().solutionFilename
    changePackageList = parser.parse_args().changePackageList
    assemblyChangePackageList = parser.parse_args().assemblyChangePackageList

    return solutionFilename, changePackageList, assemblyChangePackageList


def getProjectInfo(projectFilename):
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
    

def applyChangeProject(projInfo, projDict, assemblyChangePackageList):
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
                firstPropertiesNode.find("AssemblyVersion").text = convAssemblyVersion(projInfo.projRefInfo.newVersion)
            versionNode = firstPropertiesNode.find("Version")
            if versionNode == None:
                versionNode = etree.SubElement(firstPropertiesNode, "Version")
                versionNode.tail = "\n"
            versionNode.text = convProjectVersion(projInfo.projRefInfo.newVersion)
            fileVersionNode = firstPropertiesNode.find("FileVersion")
            if fileVersionNode == None:
                fileVersionNode = etree.SubElement(firstPropertiesNode, "FileVersion")
                fileVersionNode.tail = "\n"
            fileVersionNode.text = convAssemblyVersion(projInfo.projRefInfo.newVersion)
            isChange = True
        else:
            try:
                f = open(projInfo.asmInfoPath, "r", encoding="utf-8")
                #[assembly: AssemblyVersion("1.0.2.0")]
                #[assembly: AssemblyFileVersion("1.0.2.0")]    
                allline = list()
                for line in f:
                    if (assemblyChangePackageList == None) or (projInfo.projRefInfo.id in assemblyChangePackageList):
                        #root.find("PropertyGroup/AssemblyVersion").text = convAssemblyVersion(projInfo.projRefInfo.newVersion)
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

    refpkgs = root.findall("ItemGroup/PackageReference", ns)
    for pkg in refpkgs: 
        refver = None
        pkgName = pkg.attrib['Include']
        for refInfo in projInfo.refPkgs:
            if refInfo.id == pkgName:
                if VersionCompare(refInfo.newVersion, refInfo.version) > 0:
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
        #doc.write(nuspecPath, encoding="utf-8", pretty_print=True, doctype='<?xml version="1.0" encoding="utf-8"?>')
        doc.write(nuspecPath, encoding="utf-8", xml_declaration=True)


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
    print(solutionFilename)
    projList = list()
    try:
        f = open(solutionFilename, "r", encoding="utf-8")
        for line in f:
            if re.match(r"\s*Project\s*\(\s*\"\{.*\}\"\s*\)", line):
                token = re.findall(r"=\s*\"([^\"]*)\"\s*,\s*\"([^\"]*)\"", line)
                if token: 
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
        if proj.projRefInfo.id != None: projectDict[proj.projRefInfo.id] = proj
        projectDict[proj.projRefInfo.projectPath] = proj


def addVersion(version, inc):
    if version == None: return inc
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


def incTailVersion(version):
    if version == None: return None
    vers = version.split("-")
    if len(vers) >= 2:
        ss = vers[1]
        d = re.findall(r"(\d+)(?!.*\d)", ss)
        if d:
            ss = version[: len(version) - len(d)]
            ss = ss + str(int(d[0]) + 1)
        else:
            ss = version + "2"
        return ss
    else:
        d = re.findall(r"(\d+)(?!.*\d)", version)
        ss = version[: len(version) - len(d)]
        ss = ss + str(int(d[0]) + 1)
        return ss


def VersionCompare(version, baseVersion):
    if version == None: return -1
    if baseVersion == None: return 1
    vers1_0 = version.split("-")
    vers1 = vers1_0[0].split(".")
    vers2_0 = baseVersion.split("-")
    vers2 = vers2_0[0].split(".")
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

    if len(vers1_0) < 2:
        if len(vers2_0) < 2: return 0
        else: return 1
    else:
        if len(vers2_0) < 2: return -1
        else:
            if vers1_0[1] == vers2_0[1]: return 0
            d1 = re.findall(r"(\d+)(?!.*\d)", vers1_0[1])
            ss1 = vers1_0[: len(vers1_0) - len(d1)]
            d2 = re.findall(r"(\d+)(?!.*\d)", vers2_0[1])
            ss2 = vers2_0[: len(vers2_0) - len(d2)]
            if ss1 == ss2:
                if d1 > d2: return 1
                elif d1 < d2: return -1
            else:
                if ss1 == "alpha": sss1 = 1
                elif ss1 == "beta": sss1 = 2
                elif ss1 == "rc": sss1 = 3
                else: sss1 = 0
                if ss2 == "alpha": sss2 = 1
                elif ss2 == "beta": sss2 = 2
                elif ss2 == "rc": sss2 = 3
                else: sss2 = 0
                if sss1 > sss2: return 1
                elif sss1 < sss2: return -1
           
    return 0


def convProjectVersion(version):
    if version == None: return "0.0.0"
    vers = version.split(".")
    l = len(vers)
    if l == 3: return version
    value = ""
    for i in range(0, 3):
        if i < 3 and i < l: v = vers[i]
        else: v = "0"
        if value != "": value = value + "."
        value = value + v
    return value


def convAssemblyVersion(version):
    if version == None: return "0.0.0.0"
    vers = version.split("-")
    version = vers[0]
    vers = version.split(".")
    l = len(vers)
    if l == 4: return version
    value = ""
    for i in range(0, 4):
        if i < 4 and i < l: v = vers[i]
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
    if not changeList: return

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
                setProjectNewVersion(proj, incTailVersion(proj.projRefInfo.version), changeList)
                chageProjCnt = chageProjCnt + 1

        if chageProjCnt <= 0: break


def applyChangeProjectList(projList, projDict, assemblyChangePackageList):
    for proj in changelist:
        if proj.projRefInfo.projectPath == None: continue
        applyChangeProject(proj, projDict, assemblyChangePackageList)


if __name__ == '__main__':
    solutionFilename, changePackageList, assemblyChangePackageList = getArguments()

    projList = getProjectInfoList(solutionFilename)

    setProjectDict(projList)

    changelist = list()
    for changeModule in changePackageList:
        changeModuleInfo = changeModule.split(" ")
        moduleName = changeModuleInfo[0]
        newVersion = changeModuleInfo[1]
        print(" * ", moduleName, newVersion)

        result = setProjectNewVersionByName(projectDict, moduleName, newVersion, changelist)
        if not result:
            proj = ProjectFileInfo()
            proj.projRefInfo.id = moduleName
            proj.projRefInfo.newVersion = newVersion
            changelist.append(proj)
    
    analizeProjectList(projList, projectDict, changelist)
        
    print("---------------------------")
    for projInfo in changelist:
        if projInfo.projRefInfo.projectPath == None: continue
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

    applyChangeProjectList(projList, projectDict, assemblyChangePackageList)

    print("All Done.")
