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

class ProjectInfo:
    #projectInfo = None #ProjectVerInfo(None, None)
    #refPkgs = None # list()
    #refPrjs = None # list()
    #frameworkinfo = None
    #projectPath = None
    #asmInfoPath = None
    def __init__(self):
        self.projRefInfo = ProjectRefInfo(None, None)
        self.refPkgs = list()
        self.refPrjs0 = list()
        self.refPrjs = list()
        self.frameworkinfo = None
        self.asmInfoPath = None
        self.isTestProject = False

def getProjectInfo(projectFilename):
    projInfo = ProjectInfo()
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
        projInfo.refPrjs0.append(os.path.abspath(os.path.join(projpath, proj.attrib['Include'])))

    return projInfo


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

    for proj in projList:
        for ref in proj.refPrjs0:
            proj0 = findProjectByPath(projList, ref)
            if proj0 != None:
                proj.refPrjs.append(proj0.projRefInfo)

    return projList


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






////////////////////
def applyChangeList(projLst, moduleName, newVersion):
    proj = getProjectInfo(projList, moduleName)
    if VersionCompare(newVersion, proj.projRefInfo.newVersion > 0:
        versionChange = True
        newVersion2 = newVersion
            versionChange = False
            for ref in proj.refPkgs:
                if ref.id == moduleName and VersionCompare(newVersion, ref.newVersion) > 0:
                    ref.newVersion = newVersion
                    versionChange = True
                    break
            for ref in proj.refPrjs:
                if ref.id == moduleName:
                    versionChange = True
                    break
        if versionChange and not proj.isTestProject:
            if newVersion2 == None:
                newVersion2 = addVersion(proj.projRefInfo.version, "0.0.1")
            proj.projRefInfo.newVersion = newVersion2
            applyChangeList(projLst, proj.projRefInfo.id, proj.projRefInfo.newVersion)
////////////////////




if __name__ == '__main__':
    if len(sys.argv) < (1+1):
        print("ApplyUpdateVersion.py <Solution File> <Module Name> <New Version>")
        exit(1)

    solutionFilename = sys.argv[1]

    projList = getProjectInfoList(solutionFilename)
    applyChangeList(projList, "DevPlatform.DB", "1.0.5")

    print("---------------------------")
    for projInfo in projList:
        print("* project info :", projInfo.projRefInfo.id, projInfo.projRefInfo.version, " => ", projInfo.projRefInfo.newVersion)
        print("  * framework info :", projInfo.frameworkinfo)
        print("  * project path :", projInfo.projRefInfo.projectPath)
        print("  * assembly info path :", projInfo.asmInfoPath)
        print("  * ref packages:")
        for proj in projInfo.refPkgs:
            print("   > ", proj.id, proj.version, " => ", proj.newVersion)
        print("  * ref projects:")
        for proj in projInfo.refPrjs:
            print("   > ", proj.id, proj.version, " => ", proj.newVersion)
        print("---------------------------")






def applyChange(projLst):
    foreach proj in projLst:
       if proj.version != proj.newVersion:
           project assembly change
           for ref in proj.references:
               if ref.version != ref.newVersion:
                   apply new ref version to project
           if exist nuspec:   
               read nuspec
               nugetModified = false     
               for depNuget in nuspec.depNugetList:
                  ref = depNuget.name in proj.refrence list
                  if ref != null:
                      if depNuget.version != ref.newVersion:
                          depNuget.version = ref.newVersion
                          nugetModified = true
                if nugetModified:
                    write nuspec


if __name__ == '__main__':
    if len(sys.argv) < (1+3):
        print("ApplyUpdateVersion.py <Solution File> <Module Name> <New Version>")
        exit(1)

    solutionFilename = sys.argv[1]
    moduleName = sys.argv[2]
    newVersion = sys.argv[3]

    [proj lst] = getProjectList(solutionFilename):
    if proj list == null:
        print("errior")
        exit(1)

    proj = getProject([projLst], moduleName)
    if proj != null and newVersion > proj.newVersion:
        getChangeList([projList], moduleName, newVersion)
        applyChange([proj lst])

     print("done")




