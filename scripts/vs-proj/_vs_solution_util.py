import sys
import os
import re
import _vs_project_util as vsproj


class SolutionFileInfo:
    #solutionFilePath = None
    #projectList = None # list()
    #projectDict = None # dict()
    def __init__(self):
        self.solutionFilePath = None
        self.projectList = None # list()
        self.projectDict = None # dict()

    def updateProjectDict(self):
        self.projectDict = dict()
        for proj in self.projectList:
            if proj.projRefInfo.id != None: self.projectDict[proj.projRefInfo.id] = proj
            if proj.projRefInfo.projectPath != None: self.projectDict[proj.projRefInfo.projectPath] = proj

    def getProject(self, packageName):
        return self.projectDict[packageName]

    def getProjectByPath(self, path):
        return self.projectDict[path]


def getSolutionInfo(solutionFilename):
    #solutionFilename = os.path.abspath(solutionFilename)
    print(solutionFilename)
    try:
        f = open(solutionFilename, "r", encoding="utf-8")

        solInfo = SolutionFileInfo()
        solInfo.solutionFilePath = solutionFilename
        
        solInfo.projectList = list()
        for line in f:
            if re.match(r"\s*Project\s*\(\s*\"\{.*\}\"\s*\)", line):
                token = re.findall(r"=\s*\"([^\"]*)\"\s*,\s*\"([^\"]*)\"", line)
                if token: 
                    projname = token[0][0]
                    filename = token[0][1]
                    if filename.endswith(".csproj"):
                        slnPath = os.path.dirname(solutionFilename)
                        filename = os.path.join(slnPath, filename)
                        solInfo.projectList.append(vsproj.getProjectInfo(filename))
    except PermissionError:
        print("error")
        return null
 
    solInfo.updateProjectDict()

    return solInfo
