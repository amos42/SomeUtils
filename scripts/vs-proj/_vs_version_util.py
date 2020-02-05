import re

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
