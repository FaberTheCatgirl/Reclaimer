from pymxs import runtime as rt
import mxsshim
import pymxs

header = 0
version = 0.0
mName = ""

inFile = ""
loaded = False

nodeInfo = rt.array()
regionInfo = rt.array()
shaderInfo = rt.array()
shaderTypes = rt.array()
boneList = rt.array()
meshList = rt.array()
treeNodes = rt.array()
usedShaders = rt.array()

mGroupInfo = rt.array()

def readHeader():
    header = rt.readlong(inFile)
    version = rt.readfloat(inFile)
    mName = rt.readstring(inFile)

def readNodes():
    nodeInfo = rt.array()

    nCount = rt.readlong(inFile)
    nAddress = rt.readlong(inFile)
    fPos = rt.ftell(inFile)

    if nCount > 0:
        rt.fseek(inFile, nAddress, rt.name("seek_set"))

        for n in range(int(1), 1 + int(nCount)):
            nName = rt.readstring(inFile)
            parentIndex = rt.readshort(inFile)
            childIndex = rt.readshort(inFile)
            siblingIndex = rt.readshort(inFile)
            pos = rt.point3(rt.readfloat(inFile), rt.readfloat(inFile), rt.readfloat(inFile))
            rot = rt.quat(rt.readfloat(inFile), rt.readfloat(inFile), rt.readfloat(inFile), rt.readfloat(inFile))
            
            # in case max decides to order by name
            pre = ""
            if n < 100:
                pre += "0"
            if n < 10:
                pre += "0"
            nName = (pre + (rt.as_type(n, rt.string)) + nName)

            rt.append(nodeInfo, rt.array(nName, parentIndex, childIndex, siblingIndex, pos, rot))

    rt.fseek(inFile, fPos, rt.name("seek_set"))

def readMarkers():
    mGroupInfo = rt.array()

    mgCount = rt.readlong(inFile)
    mgAddress = rt.readlong(inFile)
    fPos = rt.ftell(inFile)

    if mgCount > 0:
        rt.fseek(inFile, mgAddress, rt.name("seek_set"))

        for mg in range(int(1), 1 + int(mgCount)):
            markers = rt.array()

            mgName = "#" + rt.readstring(inFile)
            mCount = rt.readlong(inFile)
            mAddress = rt.readlong(inFile)
            cPos = rt.ftell(inFile)

            if mCount > 0:
                rt.fseek(inFile, mAddress, rt.name("seek_set"))

                for m in range(int(1), 1 + int(mCount)):
                    rIndex = rt.readbyte(inFile)
                    pIndex = rt.readbyte(inFile)
                    nIndex = rt.readshort(inFile)
                    pos = rt.point3(rt.readfloat(inFile), rt.readfloat(inFile), rt.readfloat(inFile))
                    rot = rt.quat(rt.readfloat(inFile), rt.readfloat(inFile), rt.readfloat(inFile), rt.readfloat(inFile))
                    rt.append(markers, rt.array(rIndex, pIndex, nIndex, pos, rot))

            rt.append(mGroupInfo, rt.array(mgName, markers))
            rt.fseek(inFile, cPos, rt.name("seek_set"))

    rt.fseek(inFile, fPos, rt.name("seek_set"))

def getVMatrix(xbounds, ybounds, zbounds):
    dMat = rt.matrix3(rt.point3(1.0 / 65535.0, 0, 0), rt.point3(0, 1.0 / 65535.0, 0), rt.point3(0, 0, 1.0 / 65535.0), rt.point3(0, 0, 0))
    bMat = rt.matrix3(rt.point3(xbounds.y - xbounds.x, 0, 0), rt.point3(0, ybounds.y - ybounds.x, 0), rt.point3(0, 0, zbounds.y - zbounds.x), rt.point3(xbounds.x, ybounds.x, zbounds.x))
    return dMat

def getTMatrix(ubounds, vbounds):
    dMat = rt.matrix3(rt.point3(1.0 / 32767.0, 0, 0), rt.point3(0, 1.0 / 32767.0, 0), rt.point3(0, 0, 1.0 / 32767.0), rt.point3(0, 0, 0))
    bMat = rt.matrix3(rt.point3(ubounds.y - ubounds.x, 0, 0), rt.point3(0, vbounds.y - vbounds.x, 0), rt.point3(0, 0, 0), rt.point3(ubounds.x, vbounds.x, 0))
    return rt.matrix3(1)

def readVertices(vFormat, cFormat, vCount):
    verts = rt.array()
    vmat = rt.matrix3(1)
    tmat = rt.matrix3(1)

    if vCount > 0:
        if cFormat > 0:
            xbounds = rt.point2(rt.readfloat(inFile), rt.readfloat(inFile))
            ybounds = rt.point2(rt.readfloat(inFile), rt.readfloat(inFile))
            zbounds = rt.point2(rt.readfloat(inFile), rt.readfloat(inFile))
            ubounds = rt.point2(rt.readfloat(inFile), rt.readfloat(inFile))
            vbounds = rt.point2(rt.readfloat(inFile), rt.readfloat(inFile))

            vmat = getVMatrix(xbounds, ybounds, zbounds)
            tmat = getTMatrix(ubounds, vbounds)

        for v in range(int(1), 1 + int(vCount)):
            if cFormat == 1:
                pos = rt.point3(rt.readshort(inFile), rt.readshort(inFile), rt.readshort(inFile))
                norm = rt.point3(0, 0, 0)
                rt.readlong(inFile)
                tex = rt.point3(rt.readshort(inFile), rt.readshort(inFile), 0.0) * tmat
            else:
                pos = rt.point3(rt.readfloat(inFile), rt.readfloat(inFile), rt.readfloat(inFile))
                norm = rt.point3(rt.readfloat(inFile), rt.readfloat(inFile), rt.readfloat(inFile))
                tex = rt.point3(rt.readfloat(inFile), rt.readfloat(inFile), 0.0)

            indices = rt.array()
            weights = rt.array()

            if vFormat == 1:
                iCount = 1
                i1 = (rt.readbyte(inFile, rt.name("unsigned"))) + 1
                i2 = (rt.readbyte(inFile, rt.name("unsigned"))) + 1
                if (i2 != 256):
                    iCount += 1
                    i3 = (rt.readbyte(inFile, rt.name("unsigned"))) + 1

                    if (i3 != 256):
                        iCount += 1
                        i4 = (rt.readbyte(inFile, rt.name("unsigned"))) + 1
                        if (i4 != 256):
                            iCount += 1

                w1 = rt.readfloat(inFile)

                indices = rt.array(i1)
                weights = rt.array(w1)

                if (iCount > 1):
                    w2 = rt.readfloat(inFile)
                    indices = rt.array(i1, i2)
                    weights = rt.array(w1, w2)
                if (iCount > 2):
                    w3 = rt.readfloat(inFile)
                    indices = rt.array(i1, i2, i3)
                    weights = rt.array(w1, w2, w3)
                if (iCount > 3):
                    w4 = rt.readfloat(inFile)
                    indices = rt.array(i1, i2, i3, i4)
                    weights = rt.array(w1, w2, w3, w4)

            if vFormat == 2:
                i1 = (rt.readbyte(inFile, rt.name("unsigned"))) + 1
                indices = rt.array(i1)
                weights = rt.array(1.0)
                i2 = (rt.readbyte(inFile, rt.name("unsigned"))) + 1

                if (i2 != 256):
                    indices = rt.array(i1, i2)
                    weights = rt.array(1.0, 1.0)
                    i3 = (rt.readbyte(inFile, rt.name("unsigned"))) + 1

                    if (i3 != 256):
                        indices = rt.array(i1, i2, i3)
                        weights = rt.array(1.0, 1.0, 1.0)
                        i4 = (rt.readbyte(inFile, rt.name("unsigned"))) + 1

                        if (i4 != 256):
                            indices = rt.array(i1, i2, i3, i4)
                            weights = rt.array(1.0, 1.0, 1.0, 1.0)
            
            rt.append(verts, rt.array(pos, norm, tex, indices, weights))

    return rt.array(verts, vmat)

def copyVertices(vFrom, cFormat):
    verts = rt.array()
    vmat = rt.matrix3(1)
    tmat = rt.matrix3(1)
    if vFrom.count > 0:
        if cFormat > 0:
            xbounds = rt.point2(rt.readfloat(inFile), rt.readfloat(inFile))
            ybounds = rt.point2(rt.readfloat(inFile), rt.readfloat(inFile))
            zbounds = rt.point2(rt.readfloat(inFile), rt.readfloat(inFile))
            ubounds = rt.point2(rt.readfloat(inFile), rt.readfloat(inFile))
            vbounds = rt.point2(rt.readfloat(inFile), rt.readfloat(inFile))

            vmat = getvmatrix(xbounds, ybounds, zbounds)
            tmat = gettmatrix(ubounds, vbounds)

    return rt.array(vFrom, vmat)

def readFaces(vLen, fCount):
    faces = rt.array()
    if fCount > 0:
        for f in range(int(1), 1 + int(fCount)):
            if vLen > 65535:
                rt.append(faces, rt.point3(rt.readlong(inFile) + 1, rt.readlong(inFile) + 1, rt.readlong(inFile) + 1))
            else:
                rt.append(faces, rt.point3(rt.readshort(inFile, rt.name("unsigned")) + 1, rt.readshort(inFile, rt.name("unsigned")) + 1, rt.readshort(inFile, rt.name("unsigned")) + 1))
    return faces

def readMeshes(sCount):
    meshes = rt.array()
    if sCount > 0:
        for s in range(int(1), 1 + int(sCount)):
            shIndx = rt.readshort(inFile) + 1
            fStart = rt.readlong(inFile) + 1
            fCount = rt.readlong(inFile)
            rt.append(meshes, rt.array(shIndx, fStart, fCount))
    
    return meshes

def readRegions():
    regionInfo = rt.array()

    rCount = rt.readlong(inFile)
    rAddress = rt.readlong(inFile)
    fPos = rt.ftell(inFile)

    if rCount > 0:
        rt.fseek(inFile, rAddress, rt.name("seek_set"))

        for r in range(int(1), 1 + int(rCount)):
            perms = rt.array()

            rName = rt.readstring(inFile)
            pCount = rt.readlong(inFile)
            pAddress = rt.readlong(inFile)
            pPos = rt.ftell(inFile)

            if pCount > 0:
                rt.fseek(inFile, pAddress, rt.name("seek_set"))

                for p in range(int(1), 1 + int(pCount)):
                    pName = rt.readstring(inFile)
                    vTemp = rt.readbyte(inFile)
                    nIndex = rt.readbyte(inFile, rt.name("unsigned"))
                    vCount = rt.readlong(inFile)
                    vAddress = rt.readlong(inFile)
                    fCount = rt.readlong(inFile)
                    fAddress = rt.readlong(inFile)
                    sCount = rt.readlong(inFile)
                    sAddress = rt.readlong(inFile)

                    vFormat = rt.bit.aND(vTemp, 15)
                    # ****** WARNING : Reserved name and capitalized as aND (L5)
                    cFormat = rt.bit.shift(rt.bit.aND(vTemp, 240), -4)
                    # ****** WARNING : Reserved name and capitalized as aND (L5)
                    
                    trnsfm = rt.matrix3(1)
                    if version >= 0.1:
                        mult = rt.readfloat(inFile)
                        if not rt.bit.isnan(mult):
                            row1 = rt.point3(rt.readfloat(inFile), rt.readfloat(inFile), rt.readfloat(inFile))
                            row2 = rt.point3(rt.readfloat(inFile), rt.readfloat(inFile), rt.readfloat(inFile))
                            row3 = rt.point3(rt.readfloat(inFile), rt.readfloat(inFile), rt.readfloat(inFile))
                            row4 = rt.point3(rt.readfloat(inFile), rt.readfloat(inFile), rt.readfloat(inFile))
                            
                            trnsfm = rt.matrix3(row1, row2, row3, row4)
                    
                    xPos = rt.ftell(inFile)
                    
                    vDone = False
                    fDone = False

                    verts = rt.array()
                    faces = rt.array()

                    # save memory, use same vertices as before if possible
                    for permX in perms:
                        if permX[7 - 1] == vAddress and not vDone:
                            verts = copyVertices(permX[4 - 1], cFormat)
                            vDone = True
                        if permX[8 - 1] == fAddress and not fDone:
                            faces = permX[5 - 1]
                            fDone = True
                        if vDone and fDone:
                            break
                    
                    if not vDone:
                        rt.fseek(inFile, vAddress, rt.name("seek_set"))
                        verts = readVertices(vFormat, cFormat, vCount)
                    
                    if not fDone:
                        rt.fseek(inFile, fAddress, rt.name("seek_set"))
                        faces = readFaces(vCount, fCount)
                    
                    rt.fseek(inFile, sAddress, rt.name("seek_set"))
                    meshes = readMeshes(sCount)
                    
                    rt.print("verts: " + (rt.as_type(verts[2 - 1], rt.string)))
                    rt.print("trnsfm: " + (rt.as_type(trnsfm, rt.string)))
                    rt.append(perms, rt.array(pName, vFormat, nIndex, verts[1 - 1], faces, meshes, vAddress, fAddress, mult, (verts[2 - 1] * trnsfm)))
                    
                    rt.fseek(inFile, xPos, rt.name("seek_set"))
            
            rt.append(regionInfo, rt.array(rName, perms))
            rt.fseek(inFile, pPos, rt.name("seek_set"))
    
    rt.fseek(inFile, fPos, rt.name("seek_set"))

def readShaders():
    shaderInfo = rt.array()
    shaderTypes = rt.array()

    sCount = rt.readlong(inFile)
    sAddress = rt.readlong(inFile)
    fPos = rt.ftell(inFile)

    if sCount > 0:
        rt.fseek(inFile, sAddress, rt.name("seek_set"))

        for s in range(int(1), 1 + int(sCount)):
            sName = rt.readstring(inFile)
            sType = 0

            # * denotes a terrain blend shader
            if sName[1 - 1] == "*":
                sType = 1
                sName = (rt.substring(sName, 2, sName.count - 1))
            
            rt.append(shaderTypes, sType)
            
            if sType == 0:
                paths = rt.array()
                uvTiles = rt.array()
                tints = rt.array()
                for i in range(int(1), 1 + int(8)):
                    rt.append(paths, rt.readstring(inFile))
                    if paths[i - 1] != "null":
                        rt.append(uvTiles, rt.point2(rt.readfloat(inFile), rt.readfloat(inFile)))
                    else:
                        rt.append(uvTiles, rt.point2(0.0, 0.0))
                
                if version >= 1.1:
                    for i in range(int(1), 1 + int(4)):
                        rt.append(tints, rt.point4(rt.readbyte(inFile, rt.name("unsigned")), rt.readbyte(inFile, rt.name("unsigned")), rt.readbyte(inFile, rt.name("unsigned")), rt.readbyte(inFile, rt.name("unsigned"))))
                else:
                    for i in range(int(1), 1 + int(4)):
                        rt.append(tints, rt.point4(0, 0, 0, 0))
                
                isTrans = (rt.readbyte(inFile) == 1)
                ccOnly = (rt.readbyte(inFile) == 1)
                rt.append(shaderInfo, rt.array(sName, paths, uvTiles, isTrans, ccOnly, tints))
            else:
                baseMaps = rt.array()
                bumpMaps = rt.array()
                detMaps = rt.array()

                baseTiles = rt.array()
                bumpTiles = rt.array()
                detTiles = rt.array()

                blendPath = rt.readstring(inFile)
                if blendPath != "null":
                    blendtile = rt.point2(rt.readfloat(inFile), rt.readfloat(inFile))
                else:
                    blendtile = rt.point2(0, 0)
                
                baseCount = rt.readbyte(inFile)
                bumpCount = rt.readbyte(inFile)
                detCount = rt.readbyte(inFile)
                
                for i in range(int(1), 1 + int(baseCount)):
                    rt.append(baseMaps, rt.readstring(inFile))
                    rt.append(baseTiles, rt.point2(rt.readfloat(inFile), rt.readfloat(inFile)))
                
                for i in range(int(1), 1 + int(bumpCount)):
                    rt.append(bumpMaps, rt.readstring(inFile))
                    rt.append(bumpTiles, rt.point2(rt.readfloat(inFile), rt.readfloat(inFile)))
                
                for i in range(int(1), 1 + int(detCount)):
                    rt.append(detMaps, rt.readstring(inFile))
                    rt.append(detTiles, rt.point2(rt.readfloat(inFile), rt.readfloat(inFile)))
                
                rt.append(shaderInfo, rt.array(sName, blendPath, blendtile, baseMaps, baseTiles, bumpMaps, bumpTiles, detMaps, detTiles))
    
    rt.fseek(inFile, fPos, rt.name("seek_set"))

def loadRegions(tv):
    treeNodes = rt.array()
    
    for reg in regionInfo:
        pNode = tv.nodes.add(reg[1 - 1])
        pNode.checked = True
        subNodes = rt.array()
        
        for perm in reg[2 - 1]:
            cNode = pNode.nodes.add(perm[1 - 1])
            cNode.checked = True
            rt.append(subNodes, cNode)
        
        rt.append(treeNodes, rt.array(pNode, subNodes))

def createBones(nodeRadius, boneEnable):
    boneList = rt.array()
    
    for b in range(int(1), 1 + int(nodeInfo.count)):
        # nodeInfo #(nName, parentIndex, childIndex, siblingIndex, pos, rot)
        iNode = nodeInfo[b - 1]
        myBone = rt.bonesys.createbone(iNode[5 - 1], rt.point3(iNode[5 - 1].x + nodeRadius, iNode[5 - 1].y, iNode[5 - 1].z), rt.point3(0, 0, 1))
        myBone.name = iNode[1 - 1]
        myBone.width = nodeRadius
        myBone.height = nodeRadius
        
        if iNode[3 - 1] == -1:
            myBone.taper = 50
        else:
            myBone.taper = 70
        
        myBone.setboneenable(False, 0)
        rt.append(boneList, myBone)
        
        if iNode[2 - 1] != -1:
            myBone.parent = boneList[iNode[2 - 1] + 1 - 1]
        with mxsshim.in_coordsys(rt.name("parent")):
            myBone.rotation = iNode[6 - 1]
        with mxsshim.in_coordsys(rt.name("parent")):
            myBone.pos = iNode[5 - 1]
    
    for b in range(int(1), 1 + int(boneList.count)):
        # nodeInfo #(nName, parentIndex, childIndex, siblingIndex, pos, rot)
        myBone = boneList[b - 1]
        nName = rt.as_type(myBone.name, rt.string)
        if myBone.children.count > 0:
            for c in range(int(1), 1 + int(myBone.children.count)):
                dist = rt.distance(myBone, myBone.children[c - 1])
                if dist > myBone.length:
                    myBone.length = dist
        else:
            myBone.length = nodeRadius
        
        if boneEnable:
            myBone.setboneenable(True, 0)
    
    rt.completeredraw()

def createMarkers(markRadius):
    for mg in range(int(1), 1 + int(mGroupInfo.count)):
        mGroup = mGroupInfo[mg - 1][2 - 1]
        for m in range(int(1), 1 + int(mGroup.count)):
            marker = rt.sphere(radius=markRadius)
            marker.name = mGroupInfo[mg - 1][1 - 1]
            
            if mGroup[m - 1][3 - 1] != -1:
                marker.parent = boneList[mGroup[m - 1][3 - 1] + 1 - 1]
            
            with mxsshim.in_coordsys(rt.name("parent")):
                marker.rotation = mGroup[m - 1][5 - 1]
            
            with mxsshim.in_coordsys(rt.name("parent")):
                marker.pos = mGroup[m - 1][4 - 1]
    
    rt.completeredraw()

def createMeshes(mrge, norm, weight, uv, unwrap, tv):
    meshList = rt.array()
    usedShaders = rt.array()
    meshDic = rt.array()
    for r in range(int(1), 1 + int(treeNodes.count)):
        rNode = treeNodes[r - 1]
        if (rNode[1 - 1].checked != True):
            continue
        
        rInfo = regionInfo[r - 1]
        rName = rInfo[1 - 1]
        rPerms = rInfo[2 - 1]
        
        for p in range(int(1), 1 + int(rNode[2 - 1].count)):
            pNode = rNode[2 - 1][p - 1]
            if (pNode.checked != True):
                continue
            
            pInfo = rPerms[p - 1]
            pName = pInfo[1 - 1]
            vFormat = pInfo[2 - 1]
            nIndex = pInfo[3 - 1]
            
            # perms #(pName, vFormat, nIndex, verts, faces, meshes, vAddress, fAddress, trnsfm)
            # meshes #(shIndx, fStart, fCount)
            # verts #(pos, norm, tex, indices, weights)
            
            for s in range(int(1), 1 + int(pInfo[6 - 1].count)):
                if s > 1 and mrge:
                    continue
                
                sInfo = pInfo[6 - 1][s - 1]
                found = False
                
                if version >= 0.1:
                    if not rt.bit.isnan(pInfo[9 - 1]):
                        # check for an existing copy of an instance, and use that as a base
                        for pair in meshDic:
                            if pair[1 - 1] == pInfo[7 - 1]:
                                newMesh = None
                                rt.maxops.clonenodes(pair[2 - 1], clonetype=rt.name("instance"), newnodes=pymxs.byref(newMesh))
                                
                                if mrge:
                                    newMesh.name = rName + ":" + pName
                                else:
                                    newMesh.name = rName + ":" + pName + ":" + (rt.as_type(s, rt.string))
                                
                                mult = pInfo[9 - 1]
                                
                                newMesh.transform = (rt.matrix3(rt.point3(mult, 0, 0), rt.point3(0, mult, 0), rt.point3(0, 0, mult), rt.point3(0, 0, 0)))
                                newMesh.transform *= pInfo[10 - 1]
                                newMesh.transform *= (rt.matrix3(rt.point3(100, 0, 0), rt.point3(0, 100, 0), rt.point3(0, 0, 100), rt.point3(0, 0, 0)))
                                
                                rt.append(meshList, newMesh)
                                found = True
                
                if found:
                    continue
                
                vertList = rt.array()
                normList = rt.array()
                uvList = rt.array()
                faceList = rt.array()
                matList = rt.array()
                weightList = rt.array()
                indexList = rt.array()
                
                # fill faces first so we have the range of vert indices to use
                if mrge:
                    for sub in pInfo[6 - 1]:
                        for f in range(int(1), 1 + int(sub[3 - 1])):
                            rt.append(matList, sub[1 - 1])
                        rt.appendifunique(usedShaders, sub[1 - 1])
                    faceList = pInfo[5 - 1]
                else:
                    for i in range(int(0), 1 + int((sInfo[3 - 1] - 1))):
                        rt.append(matList, sInfo[1 - 1])
                        rt.append(faceList, pInfo[5 - 1][sInfo[2 - 1] + i - 1])
                    rt.appendifunique(usedShaders, sInfo[1 - 1])
                
                if mrge:
                    for v in pInfo[4 - 1]:
                        rt.append(vertList, v[1 - 1])
                        rt.append(normList, v[2 - 1])
                        rt.append(uvList, v[3 - 1])
                        if nIndex == 255:
                            rt.append(indexList, v[4 - 1])
                            rt.append(weightList, v[5 - 1])
                        else:
                            rt.append(indexList, rt.array(nIndex + 1))
                            rt.append(weightList, rt.array(1))
                else:
                    # fill the vertList only with verts in the range of the face indices, and shift the indices to start at 1
                    vMin = pInfo[4 - 1].count
                    vMax = 1
                    for i in range(int(1), 1 + int(faceList.count)):
                        if faceList[i - 1].x < vMin:
                            vMin = faceList[i - 1].x
                        if faceList[i - 1].y < vMin:
                            vMin = faceList[i - 1].y
                        if faceList[i - 1].z < vMin:
                            vMin = faceList[i - 1].z
                        if faceList[i - 1].x > vMax:
                            vMax = faceList[i - 1].x
                        if faceList[i - 1].y > vMax:
                            vMax = faceList[i - 1].y
                        if faceList[i - 1].z > vMax:
                            vMax = faceList[i - 1].z
                    for i in range(int(vMin), 1 + int(vMax)):
                        rt.append(vertList, pInfo[4 - 1][i - 1][1 - 1])
                        rt.append(normList, pInfo[4 - 1][i - 1][2 - 1])
                        rt.append(uvList, pInfo[4 - 1][i - 1][3 - 1])
                        if nIndex == 255:
                            rt.append(indexList, pInfo[4 - 1][i - 1][4 - 1])
                            rt.append(weightList, pInfo[4 - 1][i - 1][5 - 1])
                        else:
                            rt.append(indexList, rt.array(nIndex + 1))
                            rt.append(weightList, rt.array(1))
                    for i in range(int(1), 1 + int(faceList.count)):
                        faceList[i - 1] -= (vMin - 1)
                
                if uv:
                    newMesh = rt.mesh(vertices=vertList, tverts=uvList, faces=faceList, materialids=matList)
                    rt.buildtvfaces(newMesh)
                    for i in range(int(1), 1 + int(newMesh.numfaces)):
                        rt.settvface(newMesh, i, rt.getface(newMesh, i))
                else:
                    newMesh = rt.mesh(vertices=vertList, faces=faceList)
                
                if mrge:
                    newMesh.name = rName + ":" + pName
                else:
                    newMesh.name = rName + ":" + pName + ":" + (rt.as_type(s, rt.string))
                
                # face group for CE region exports
                newMesh.faces[rName] = newMesh.faces
                
                if norm:
                    mxsshim.max("modify mode")
                    normMod = rt.edit_normals()
                    rt.select(newMesh)
                    rt.addmodifier(newMesh, normMod)
                    normMod.selectby = 1
                    normMod.sellevel = rt.name("Vertex")
                    normMod.unify(selection=rt.bitarray(*(list(range(1, normMod.getnumnormals() + 1)))))
                    normMod.makeexplicit(selection=rt.bitarray(*(list(range(1, normMod.getnumnormals() + 1)))))
                    for i in range(int(1), 1 + int(newMesh.numverts)):
                        sel = rt.bitarray(*([i]))
                        norms = rt.bitarray(*())
                        (_, sel, norms) = normMod.convertvertexselection(pymxs.byref(sel), pymxs.byref(norms))
                        
                        for j in norms:
                            normMod.setnormal(j, normList[i - 1])
                    
                    # dont want annoying modifier with lines everywhere
                    rt.collapsestack(newMesh)
                
                if version >= 0.1:
                    if not rt.bit.isnan(pInfo[9 - 1]):
                        mult = pInfo[9 - 1]
                        newMesh.transform = (rt.matrix3(rt.point3(mult, 0, 0), rt.point3(0, mult, 0), rt.point3(0, 0, mult), rt.point3(0, 0, 0)))
                        newMesh.transform *= pInfo[10 - 1]
                        newMesh.transform *= (rt.matrix3(rt.point3(100, 0, 0), rt.point3(0, 100, 0), rt.point3(0, 0, 100), rt.point3(0, 0, 0)))
                
                if unwrap:
                    uvMod = rt.unwrap_uvw()
                    rt.addmodifier(newMesh, uvMod)
                
                if (vFormat > 0 or nIndex != 255) and weight:
                    mxsshim.max("modify mode")
                    theSkin = rt.skin()
                    rt.select(newMesh)
                    rt.addmodifier(newMesh, theSkin)
                    for b in boneList:
                        rt.skinops.addbone(theSkin, b, 0)
                    rt.redrawviews()
                    for v in range(int(1), 1 + int(vertList.count)):
                        rt.skinops.replacevertexweights(theSkin, v, indexList[v - 1], weightList[v - 1])
                    
                    if vFormat == 2 or nIndex != 255:
                        theSkin.rigid_vertices = True
                
                rt.append(meshList, newMesh)
                rt.append(meshDic, rt.array(pInfo[7 - 1], newMesh))
    
    rt.completeredraw()