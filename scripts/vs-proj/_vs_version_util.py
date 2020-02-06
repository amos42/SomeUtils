import re
import copy


#^(?P<major>0|[1-9]\d*)\.(?P<minor>0|[1-9]\d*)\.(?P<patch>0|[1-9]\d*)(?:-(?P<prerelease>(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+(?P<buildmetadata>[0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$

class SemVersion:
    #core = None # list()
    #preRelease = None
    def __init__(self):
        self.core = None
        self.preRelease = None # list()

    def __init__(self, versionStr, coreLen = 0):
        self.setVersionString(versionStr)

    def clone(self):
        newVer = copy.deepcopy(self)
        return newVer

    def setVersionString(self, versionStr, coreLen = 0):
        if not versionStr: 
            self.core = [1]
            self.preRelease = None
            return
        vers = versionStr.split('-')
        if len(vers) >= 2:
            self.preRelease = vers[1]
        else:
            self.preRelease = None
        vers = vers[0].split('.')
        self.core = list()
        for v in vers:
            self.core.append(int(v))

    def toString(self, coreLen = 0):
        verStr = ""
        if coreLen == 0: coreLen = len(self.core)
        minlen = min(len(self.core), coreLen)
        idx = 0
        for v in range(0, minlen):
            if idx > 0: verStr = verStr + '.'
            verStr = verStr + str(self.core[v])
            idx += 1
        if idx < coreLen:
            for i in range(idx, coreLen):
                verStr = verStr + ".0"
        if self.preRelease:
            verStr = verStr + '-' + self.preRelease    
        return verStr

    def addVersion(self, incVer):
        l1 = len(self.core)
        l2 = len(incVer.core)
        for i in range(0, l1):
            if i < l2: self.core[i] += incVer.core[i]

    def incTailVersion(self, inc = 1):
        if self.preRelease:
            d = re.findall(r"(\d+)(?!.*\d)", self.preRelease)
            if d:
                ss = self.preRelease[: len(self.preRelease) - len(d)]
                ss = ss + str(int(d[0]) + inc)
            else:
                ss = self.preRelease + str(1 + inc)
            self.preRelease = ss
        else:
            v = self.core[len(self.core) - 1]
            self.core[len(self.core) - 1] = v + inc


def VersionCompare(version, baseVersion):
    if not version and not baseVersion: return 0
    if not version: return -1
    if not baseVersion: return 1

    l1 = len(version.core)
    l2 = len(baseVersion.core)
    l3 = max(l1, l2)
    for i in range(0, l3):
        if i < l1: v1 = version.core[i]
        else: v1 = 0        
        if i < l2: v2 = baseVersion.core[i]
        else: v2 = 0
        if v1 > v2: return 1
        elif v1 < v2: return -1

    if version.preRelease:
        if not baseVersion.preRelease: return 1
        else:
            if version.preRelease > baseVersion.preRelease: return 1
            elif version.preRelease < baseVersion.preRelease: return -1
    elif version.preRelease: return -1
        
    return 0
