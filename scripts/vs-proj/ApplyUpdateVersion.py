import sys
import os
import argparse
import _vs_project_util as vsproj
import _vs_solution_util as vssol
import _vs_version_util as vsver


def getArguments():
    parser = argparse.ArgumentParser()
    parser.add_argument(dest='solutionFilename', help='Example) Sample.sln')
    parser.add_argument('--change-packages', '-c', nargs='+', default=[], metavar='"PackageName Version"', dest='changePackageList', help='Example) "DevPlatfomr.Base 1.0.1" "DevPlatfomr.DB 1.0.6"')
    parser.add_argument('--assembly-change-packages', '-a', nargs='*', default=[], metavar='"PackageName"', dest='assemblyChangePackageList', help='Example) DevPlatfomr.Base DevPlatfomr.DB DevPlatfomr.DB.*')

    solutionFilename = parser.parse_args().solutionFilename
    changePackageList = parser.parse_args().changePackageList
    assemblyChangePackageList = parser.parse_args().assemblyChangePackageList

    return solutionFilename, changePackageList, assemblyChangePackageList


def setNewVersion(refInfo, newVersion):
    if (newVersion == None) or not newVersion.core: return False
    if (refInfo.newVersion != None) and not refInfo.newVersion.core: curVersion = refInfo.newVersion
    else: curVersion = refInfo.version
    if vsver.VersionCompare(newVersion, curVersion) > 0:
        refInfo.newVersion = newVersion.clone()
        return True
    else:
        return False


def setProjectNewVersion(project, newVersion, changeList):
    result = setNewVersion(project.projRefInfo, newVersion)
    if result == True:
        if not (project in changeList):
            changeList.append(project)
    return result


def setProjectNewVersionByName(projDict, moduleName, newVersion, changeList):
    proj = projDict.get(moduleName)
    if proj == None: return False
    return setProjectNewVersion(proj, newVersion, changeList)


def applyChangeProjectList(changeList, projDict, assemblyChangePackageList):
    for proj in changeList:
        if proj.projRefInfo.projectPath == None: continue
        vsproj.updateProjectInfo(proj, projDict, assemblyChangePackageList)


def analizeProjectList(projList, projDict, changeList):
    if not changeList: return

    while True:
        changeProjCnt = 0

        for proj in projList:
            if proj in changeList: continue

            changeCnt = 0
            for ref in proj.refPkgs:
                for prj2 in changeList:
                    if ref.id == prj2.projRefInfo.id:
                        if setNewVersion(ref, prj2.projRefInfo.newVersion): changeCnt = changeCnt + 1
                        break
            if changeCnt <= 0:
                for ref in proj.refPrjs:
                    proj2 = projDict[ref]
                    if proj2 in changeList: 
                        changeCnt = changeCnt + 1
                        break
            if changeCnt > 0:
                if proj.projRefInfo.version != None:
                    newVersion = proj.projRefInfo.version.clone()
                    newVersion.incTailVersion()
                else:
                    newVersion = vsver.SemVersion("1.0.1")
                if setProjectNewVersion(proj, newVersion, changeList):
                    changeProjCnt = changeProjCnt + 1

        if changeProjCnt <= 0: break


if __name__ == '__main__':
    solutionFilename, changePackageList, assemblyChangePackageList = getArguments()

    solInfo = vssol.getSolutionInfo(solutionFilename)

    changelist = list()
    for changeModule in changePackageList:
        changeModuleInfo = changeModule.split(" ")
        moduleName = changeModuleInfo[0]
        newVersion = vsver.SemVersion(changeModuleInfo[1])
        print("> ", moduleName, newVersion.toString())

        result = setProjectNewVersionByName(solInfo.projectDict, moduleName, newVersion, changelist)
        if not result:
            proj = vsproj.ProjectFileInfo()
            proj.projRefInfo.id = moduleName
            proj.projRefInfo.newVersion = newVersion
            changelist.append(proj)
    
    analizeProjectList(solInfo.projectList, solInfo.projectDict, changelist)
        
    print("---------------------------")
    for projInfo in changelist:
        if not projInfo.projRefInfo.projectPath: continue
        if (projInfo.projRefInfo.newVersion == None) or (not projInfo.projRefInfo.newVersion.core): continue
        print("* project info :", projInfo.projRefInfo.toString())
        print("  * framework info :", projInfo.frameworkinfo)
        print("  * project path :", projInfo.projRefInfo.projectPath)
        print("  * assembly info path :", projInfo.asmInfoPath)
        print("  * ref packages:")
        for pkg in projInfo.refPkgs:
            print("   >", pkg.toString())
        print("  * ref projects:")
        for proj0 in projInfo.refPrjs:
            proj = solInfo.projectDict[proj0].projRefInfo
            print("   >", proj.toString())
        print("  * test project :", projInfo.isTestProject)
        print("---------------------------")

    applyChangeProjectList(changelist, solInfo.projectDict, assemblyChangePackageList)

    print("All Done.")
