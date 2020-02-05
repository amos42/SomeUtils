import sys
import os
import re
import shutil
import argparse
import xml.etree.ElementTree as etree
import _vs_proj_util as vsproj
import _vs_version_util as vsver


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


def setProjectDict(projList, projDict):
    projDict.clear()
    for proj in projList:
        if proj.projRefInfo.id != None: projDict[proj.projRefInfo.id] = proj
        projDict[proj.projRefInfo.projectPath] = proj


def setNewVersion(refInfo, newVersion):
    if newVersion == None: return False
    curVersion = refInfo.newVersion
    if curVersion == None: curVersion = refInfo.version
    if vsver.VersionCompare(newVersion, curVersion) > 0:
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
                setProjectNewVersion(proj, vsver.incTailVersion(proj.projRefInfo.version), changeList)
                chageProjCnt = chageProjCnt + 1

        if chageProjCnt <= 0: break


if __name__ == '__main__':
    solutionFilename, changePackageList, assemblyChangePackageList = getArguments()

    projList = vsproj.getProjectInfoList(solutionFilename)

    setProjectDict(projList, projectDict)

    changelist = list()
    for changeModule in changePackageList:
        changeModuleInfo = changeModule.split(" ")
        moduleName = changeModuleInfo[0]
        newVersion = changeModuleInfo[1]
        print("> ", moduleName, newVersion)

        result = setProjectNewVersionByName(projectDict, moduleName, newVersion, changelist)
        if not result:
            proj = vsproj.ProjectFileInfo()
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

    vsproj.applyChangeProjectList(changelist, projectDict, assemblyChangePackageList)

    print("All Done.")
