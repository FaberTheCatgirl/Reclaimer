from typing import List, Tuple
import math

from pymxs import runtime as rt

import mxsshim
import pymxs

header = 0
version = 0.0
mName = ""

inFile = ""
loaded = False

T_Point2 = Tuple[float, float]
T_Point3 = Tuple[float, float, float]
T_Point4 = Tuple[float, float, float, float]

T_VertInfo = Tuple[object, object, object, List[T_Point4], List[T_Point4]]

T_NodeInfo = Tuple[str, int, int, int, object, object]
T_MeshInfo = Tuple[int, int, int]
T_PermInfo = Tuple[str, int, int, List[T_VertInfo], List[T_Point3], List[T_MeshInfo], int, int, object]
T_RegionInfo = Tuple[str, List[T_PermInfo]]
T_ShaderInfo = Tuple[str, List[str], List[T_Point2], bool, bool, List[T_Point4]]

T_MarkerInfo = Tuple[int, int, int, object, object]
T_MGroupInfo = Tuple[str, List[T_MarkerInfo]]

nodeInfo: List[T_NodeInfo] = []
regionInfo: List[T_RegionInfo] = []
shaderInfo: List[T_ShaderInfo] = []
shaderTypes: List[int] = []
boneList = []
meshList = []
treeNodes = []
usedShaders = []

mGroupInfo: List[T_MGroupInfo] = []

def readHeader():
    header = rt.ReadLong(inFile)
    version = rt.ReadFloat(inFile)
    mName = rt.ReadString(inFile)

def readNodes():
    nCount = rt.ReadLong(inFile)
    nAddress = rt.ReadLong(inFile)
    fPos = rt.Ftell(inFile)

    if nCount > 0:
        rt.Fseek(inFile, nAddress, rt.Name("seek_set"))

        for n in range(nCount):
            nName = rt.ReadString(inFile)
            parentIndex = rt.ReadShort(inFile)
            childIndex = rt.ReadShort(inFile)
            siblingIndex = rt.ReadShort(inFile)
            pos = rt.Point3(rt.ReadFloat(inFile), rt.ReadFloat(inFile), rt.ReadFloat(inFile))
            rot = rt.Quat(rt.ReadFloat(inFile), rt.ReadFloat(inFile), rt.ReadFloat(inFile), rt.ReadFloat(inFile))
            
            # in case max decides to order by name
            nName = f"{n:000}{nName}"

            nodeInfo.append((nName, parentIndex, childIndex, siblingIndex, pos, rot))

    rt.Fseek(inFile, fPos, rt.Name("seek_set"))

def readMarkers():
    mgCount = rt.ReadLong(inFile)
    mgAddress = rt.ReadLong(inFile)
    fPos = rt.Ftell(inFile)

    if mgCount > 0:
        rt.Fseek(inFile, mgAddress, rt.Name("seek_set"))

        for mg in range(mgCount):
            markers: List[T_MarkerInfo] = []

            mgName = "#" + rt.ReadString(inFile)
            mCount = rt.ReadLong(inFile)
            mAddress = rt.ReadLong(inFile)
            cPos = rt.Ftell(inFile)

            if mCount > 0:
                rt.Fseek(inFile, mAddress, rt.Name("seek_set"))

                for m in range(mCount):
                    rIndex = rt.ReadByte(inFile)
                    pIndex = rt.ReadByte(inFile)
                    nIndex = rt.ReadShort(inFile)
                    pos = rt.Point3(rt.ReadFloat(inFile), rt.ReadFloat(inFile), rt.ReadFloat(inFile))
                    rot = rt.Quat(rt.ReadFloat(inFile), rt.ReadFloat(inFile), rt.ReadFloat(inFile), rt.ReadFloat(inFile))
                    markers.append((rIndex, pIndex, nIndex, pos, rot))

            mGroupInfo.append((mgName, markers))
            rt.Fseek(inFile, cPos, rt.Name("seek_set"))

    rt.Fseek(inFile, fPos, rt.Name("seek_set"))

def getVMatrix(xbounds, ybounds, zbounds):
    dMat = rt.Matrix3(rt.Point3(1.0 / 65535.0, 0, 0), rt.Point3(0, 1.0 / 65535.0, 0), rt.Point3(0, 0, 1.0 / 65535.0), rt.Point3(0, 0, 0))
    bMat = rt.Matrix3(rt.Point3(xbounds.y - xbounds.x, 0, 0), rt.Point3(0, ybounds.y - ybounds.x, 0), rt.Point3(0, 0, zbounds.y - zbounds.x), rt.Point3(xbounds.x, ybounds.x, zbounds.x))
    return dMat

def getTMatrix(ubounds, vbounds):
    dMat = rt.Matrix3(rt.Point3(1.0 / 32767.0, 0, 0), rt.Point3(0, 1.0 / 32767.0, 0), rt.Point3(0, 0, 1.0 / 32767.0), rt.Point3(0, 0, 0))
    bMat = rt.Matrix3(rt.Point3(ubounds.y - ubounds.x, 0, 0), rt.Point3(0, vbounds.y - vbounds.x, 0), rt.Point3(0, 0, 0), rt.Point3(ubounds.x, vbounds.x, 0))
    return rt.Matrix3(1)

def readVertices(vFormat: int, cFormat: int, vCount: int) -> Tuple[T_VertInfo, object]:
    verts: List[T_VertInfo] = []
    vmat = rt.Matrix3(1)
    tmat = rt.Matrix3(1)

    if vCount > 0:
        if cFormat > 0:
            xbounds = rt.Point2(rt.ReadFloat(inFile), rt.ReadFloat(inFile))
            ybounds = rt.Point2(rt.ReadFloat(inFile), rt.ReadFloat(inFile))
            zbounds = rt.Point2(rt.ReadFloat(inFile), rt.ReadFloat(inFile))
            ubounds = rt.Point2(rt.ReadFloat(inFile), rt.ReadFloat(inFile))
            vbounds = rt.Point2(rt.ReadFloat(inFile), rt.ReadFloat(inFile))

            vmat = getVMatrix(xbounds, ybounds, zbounds)
            tmat = getTMatrix(ubounds, vbounds)

        for v in range(vCount):
            if cFormat == 1:
                pos = rt.Point3(rt.ReadShort(inFile), rt.ReadShort(inFile), rt.ReadShort(inFile))
                norm = rt.Point3(0, 0, 0)
                rt.ReadLong(inFile)
                tex = rt.Point3(rt.ReadShort(inFile), rt.ReadShort(inFile), 0.0) * tmat
            else:
                pos = rt.Point3(rt.ReadFloat(inFile), rt.ReadFloat(inFile), rt.ReadFloat(inFile))
                norm = rt.Point3(rt.ReadFloat(inFile), rt.ReadFloat(inFile), rt.ReadFloat(inFile))
                tex = rt.Point3(rt.ReadFloat(inFile), rt.ReadFloat(inFile), 0.0)

            indices = []
            weights = []

            if vFormat == 1:
                iCount = 1
                i1 = rt.ReadByte(inFile, rt.Name("unsigned"))
                i2 = rt.ReadByte(inFile, rt.Name("unsigned"))
                if i2 != 256:
                    iCount += 1
                    i3 = rt.ReadByte(inFile, rt.Name("unsigned"))

                    if i3 != 256:
                        iCount += 1
                        i4 = rt.ReadByte(inFile, rt.Name("unsigned"))
                        if i4 != 256:
                            iCount += 1

                w1 = rt.ReadFloat(inFile)

                indices = (i1,)
                weights = (w1,)

                if iCount > 1:
                    w2 = rt.ReadFloat(inFile)
                    indices = (i1, i2)
                    weights = (w1, w2)
                if iCount > 2:
                    w3 = rt.ReadFloat(inFile)
                    indices = (i1, i2, i3)
                    weights = (w1, w2, w3)
                if iCount > 3:
                    w4 = rt.ReadFloat(inFile)
                    indices = (i1, i2, i3, i4)
                    weights = (w1, w2, w3, w4)

            if vFormat == 2:
                i1 = rt.ReadByte(inFile, rt.Name("unsigned"))
                indices = (i1,)
                weights = (1.0,)
                i2 = rt.ReadByte(inFile, rt.Name("unsigned"))

                if i2 != 256:
                    indices = (i1, i2)
                    weights = (1.0, 1.0)
                    i3 = rt.ReadByte(inFile, rt.Name("unsigned"))

                    if i3 != 256:
                        indices = (i1, i2, i3)
                        weights = (1.0, 1.0, 1.0)
                        i4 = rt.ReadByte(inFile, rt.Name("unsigned"))

                        if i4 != 256:
                            indices = (i1, i2, i3, i4)
                            weights = (1.0, 1.0, 1.0, 1.0)
            
            verts.append((pos, norm, tex, indices, weights))

    return (verts, vmat)

def copyVertices(vFrom, cFormat: int):
    verts = []
    vmat = rt.Matrix3(1)
    tmat = rt.Matrix3(1)
    if vFrom.count > 0:
        if cFormat > 0:
            xbounds = rt.Point2(rt.ReadFloat(inFile), rt.ReadFloat(inFile))
            ybounds = rt.Point2(rt.ReadFloat(inFile), rt.ReadFloat(inFile))
            zbounds = rt.Point2(rt.ReadFloat(inFile), rt.ReadFloat(inFile))
            ubounds = rt.Point2(rt.ReadFloat(inFile), rt.ReadFloat(inFile))
            vbounds = rt.Point2(rt.ReadFloat(inFile), rt.ReadFloat(inFile))

            vmat = getVMatrix(xbounds, ybounds, zbounds)
            tmat = getTMatrix(ubounds, vbounds)

    return rt.Array(vFrom, vmat)

def readFaces(vLen: int, fCount: int) -> List[T_Point3]:
    faces = []
    if fCount > 0:
        for f in range(fCount):
            if vLen > 65535:
                faces.append(rt.Point3(rt.ReadLong(inFile), rt.ReadLong(inFile), rt.ReadLong(inFile)))
            else:
                faces.append(rt.Point3(rt.ReadShort(inFile, rt.Name("unsigned")), rt.ReadShort(inFile, rt.Name("unsigned")), rt.ReadShort(inFile, rt.Name("unsigned"))))
    return faces

def readMeshes(sCount: int) -> List[T_MeshInfo]:
    meshes: List[T_MeshInfo] = []
    if sCount > 0:
        for _ in range(sCount):
            shIndx = rt.ReadShort(inFile)
            fStart = rt.ReadLong(inFile)
            fCount = rt.ReadLong(inFile)
            meshes.append((shIndx, fStart, fCount))
    
    return meshes

def readRegions():
    rCount = rt.ReadLong(inFile)
    rAddress = rt.ReadLong(inFile)
    fPos = rt.Ftell(inFile)

    if rCount > 0:
        rt.Fseek(inFile, rAddress, rt.Name("seek_set"))

        for r in range(rCount):
            perms: List[T_PermInfo] = []

            rName = rt.ReadString(inFile)
            pCount = rt.ReadLong(inFile)
            pAddress = rt.ReadLong(inFile)
            pPos = rt.Ftell(inFile)

            if pCount > 0:
                rt.Fseek(inFile, pAddress, rt.Name("seek_set"))

                for p in range(pCount):
                    pName = rt.ReadString(inFile)
                    vTemp = rt.ReadByte(inFile)
                    nIndex = rt.ReadByte(inFile, rt.Name("unsigned"))
                    vCount = rt.ReadLong(inFile)
                    vAddress = rt.ReadLong(inFile)
                    fCount = rt.ReadLong(inFile)
                    fAddress = rt.ReadLong(inFile)
                    sCount = rt.ReadLong(inFile)
                    sAddress = rt.ReadLong(inFile)

                    vFormat = vTemp & 15
                    cFormat = (vTemp & 240) << 4
                    
                    trnsfm = rt.Matrix3(1)
                    if version >= 0.1:
                        mult = rt.ReadFloat(inFile)
                        if not math.isnan(mult):
                            row1 = rt.Point3(rt.ReadFloat(inFile), rt.ReadFloat(inFile), rt.ReadFloat(inFile))
                            row2 = rt.Point3(rt.ReadFloat(inFile), rt.ReadFloat(inFile), rt.ReadFloat(inFile))
                            row3 = rt.Point3(rt.ReadFloat(inFile), rt.ReadFloat(inFile), rt.ReadFloat(inFile))
                            row4 = rt.Point3(rt.ReadFloat(inFile), rt.ReadFloat(inFile), rt.ReadFloat(inFile))
                            
                            trnsfm = rt.Matrix3(row1, row2, row3, row4)
                    
                    xPos = rt.Ftell(inFile)
                    
                    vDone = False
                    fDone = False

                    verts = []
                    faces = []

                    # save memory, use same vertices as before if possible
                    for permX in perms:
                        if permX[6] == vAddress and not vDone:
                            verts = copyVertices(permX[3], cFormat)
                            vDone = True
                        if permX[7] == fAddress and not fDone:
                            faces = permX[4]
                            fDone = True
                        if vDone and fDone:
                            break
                    
                    if not vDone:
                        rt.Fseek(inFile, vAddress, rt.Name("seek_set"))
                        verts = readVertices(vFormat, cFormat, vCount)
                    
                    if not fDone:
                        rt.Fseek(inFile, fAddress, rt.Name("seek_set"))
                        faces = readFaces(vCount, fCount)
                    
                    rt.Fseek(inFile, sAddress, rt.Name("seek_set"))
                    meshes = readMeshes(sCount)
                    
                    rt.Print(f"verts: {verts[1]}")
                    rt.Print(f"trnsfm: {trnsfm}")
                    perms.append((pName, vFormat, nIndex, verts[0], faces, meshes, vAddress, fAddress, mult, (verts[1] * trnsfm)))
                    
                    rt.Fseek(inFile, xPos, rt.Name("seek_set"))
            
            regionInfo.append((rName, perms))
            rt.Fseek(inFile, pPos, rt.Name("seek_set"))
    
    rt.Fseek(inFile, fPos, rt.Name("seek_set"))

def readShaders():
    sCount = rt.ReadLong(inFile)
    sAddress = rt.ReadLong(inFile)
    fPos = rt.Ftell(inFile)

    if sCount > 0:
        rt.Fseek(inFile, sAddress, rt.Name("seek_set"))

        for s in range(sCount):
            sName = rt.ReadString(inFile)
            sType = 0

            # * denotes a terrain blend shader
            if sName[0] == "*":
                sType = 1
                sName = rt.Substring(sName, 2, sName.count - 1)
            
            shaderTypes.append(sType)
            
            if sType == 0:
                paths = []
                uvTiles = []
                tints = []
                for i in range(8):
                    paths.append(rt.ReadString(inFile))
                    if paths[i] != "null":
                        uvTiles.append(rt.Point2(rt.ReadFloat(inFile), rt.ReadFloat(inFile)))
                    else:
                        uvTiles.append(rt.Point2(0.0, 0.0))
                
                if version >= 1.1:
                    for _ in range(4):
                        tints.append(rt.Point4(rt.ReadByte(inFile, rt.Name("unsigned")), rt.ReadByte(inFile, rt.Name("unsigned")), rt.ReadByte(inFile, rt.Name("unsigned")), rt.ReadByte(inFile, rt.Name("unsigned"))))
                else:
                    for _ in range(4):
                        tints.append(rt.Point4(0, 0, 0, 0))
                
                isTrans = rt.ReadByte(inFile) == 1
                ccOnly = rt.ReadByte(inFile) == 1
                shaderInfo.append((sName, paths, uvTiles, isTrans, ccOnly, tints))
            else:
                baseMaps: List[str] = []
                bumpMaps: List[str] = []
                detMaps: List[str] = []

                baseTiles = []
                bumpTiles = []
                detTiles = []

                blendPath = rt.ReadString(inFile)
                if blendPath != "null":
                    blendtile = rt.Point2(rt.ReadFloat(inFile), rt.ReadFloat(inFile))
                else:
                    blendtile = rt.Point2(0, 0)
                
                baseCount = rt.ReadByte(inFile)
                bumpCount = rt.ReadByte(inFile)
                detCount = rt.ReadByte(inFile)
                
                for _ in range(baseCount):
                    baseMaps.append(rt.ReadString(inFile))
                    baseTiles.append(rt.Point2(rt.ReadFloat(inFile), rt.ReadFloat(inFile)))
                
                for _ in range(bumpCount):
                    bumpMaps.append(rt.ReadString(inFile))
                    bumpTiles.append(rt.Point2(rt.ReadFloat(inFile), rt.ReadFloat(inFile)))
                
                for _ in range(detCount):
                    detMaps.append(rt.ReadString(inFile))
                    detTiles.append(rt.Point2(rt.ReadFloat(inFile), rt.ReadFloat(inFile)))
                
                shaderInfo.append((sName, blendPath, blendtile, baseMaps, baseTiles, bumpMaps, bumpTiles, detMaps, detTiles))
    
    rt.Fseek(inFile, fPos, rt.Name("seek_set"))

def loadRegions(tv):
    treeNodes = []
    
    for reg in regionInfo:
        pNode = tv.nodes.add(reg[0])
        pNode.checked = True
        subNodes = []
        
        for perm in reg[1]:
            cNode = pNode.nodes.add(perm[0])
            cNode.checked = True
            subNodes.append(cNode)
        
        treeNodes.append(rt.Array(pNode, subNodes))

def createBones(nodeRadius: float, boneEnable: bool):
    for b in range(nodeInfo.count):
        # nodeInfo #(nName, parentIndex, childIndex, siblingIndex, pos, rot)
        iNode = nodeInfo[b]
        myBone = rt.BoneSys.createbone(iNode[4], rt.Point3(iNode[4].x + nodeRadius, iNode[4].y, iNode[4].z), rt.Point3(0, 0, 1))
        myBone.name = iNode[0]
        myBone.width = nodeRadius
        myBone.height = nodeRadius
        
        if iNode[2] == -1:
            myBone.taper = 50
        else:
            myBone.taper = 70
        
        myBone.setboneenable(False, 0)
        boneList.append(myBone)
        
        if iNode[1] != -1:
            myBone.parent = boneList[iNode[1]]
        with mxsshim.in_coordsys(rt.Name("parent")):
            myBone.rotation = iNode[5]
        with mxsshim.in_coordsys(rt.Name("parent")):
            myBone.pos = iNode[4]
    
    for b in range(boneList.count):
        # nodeInfo #(nName, parentIndex, childIndex, siblingIndex, pos, rot)
        myBone = boneList[b]
        nName = str(myBone.name)
        if myBone.children.count > 0:
            for c in range(myBone.children.count):
                dist = rt.Distance(myBone, myBone.children[c])
                if dist > myBone.length:
                    myBone.length = dist
        else:
            myBone.length = nodeRadius
        
        if boneEnable:
            myBone.setboneenable(True, 0)
    
    rt.CompleteRedraw()

def createMarkers(markRadius):
    for mg in range(mGroupInfo.count):
        mGroup = mGroupInfo[mg][1]
        for m in range(mGroup.count):
            marker = rt.Sphere(radius=markRadius)
            marker.name = mGroupInfo[mg][0]
            
            if mGroup[m][2] != -1:
                marker.parent = boneList[mGroup[m][2]]
            
            with mxsshim.in_coordsys(rt.Name("parent")):
                marker.rotation = mGroup[m][4]
            
            with mxsshim.in_coordsys(rt.Name("parent")):
                marker.pos = mGroup[m][3]
    
    rt.CompleteRedraw()

def createMeshes(mrge: bool, norm: bool, weight: bool, uv: bool, unwrap: bool, tv):
    meshDic = []
    for r in range(treeNodes.count):
        rNode = treeNodes[r]
        if rNode[0].checked != True:
            continue
        
        rInfo = regionInfo[r]
        rName = rInfo[0]
        rPerms = rInfo[1]
        
        for p in range(rNode[1].count):
            pNode = rNode[1][p]
            if pNode.checked != True:
                continue
            
            pInfo = rPerms[p]
            pName = pInfo[0]
            vFormat = pInfo[1]
            nIndex = pInfo[2]
            
            # perms #(pName, vFormat, nIndex, verts, faces, meshes, vAddress, fAddress, trnsfm)
            # meshes #(shIndx, fStart, fCount)
            # verts #(pos, norm, tex, indices, weights)
            
            for s in range(pInfo[5].count):
                if s > 0 and mrge:
                    continue
                
                sInfo = pInfo[5][s]
                found = False
                
                if version >= 0.1:
                    if not math.isnan(pInfo[8]):
                        # check for an existing copy of an instance, and use that as a base
                        for pair in meshDic:
                            if pair[0] == pInfo[6]:
                                newMesh = None
                                rt.maxops.clonenodes(pair[1], clonetype=rt.Name("instance"), newnodes=pymxs.byref(newMesh))
                                
                                if mrge:
                                    newMesh.name = rName + ":" + pName
                                else:
                                    newMesh.name = rName + ":" + pName + ":" + str(s)
                                
                                mult = pInfo[8]
                                
                                newMesh.transform = rt.Matrix3(rt.Point3(mult, 0, 0), rt.Point3(0, mult, 0), rt.Point3(0, 0, mult), rt.Point3(0, 0, 0))
                                newMesh.transform *= pInfo[9]
                                newMesh.transform *= rt.Matrix3(rt.Point3(100, 0, 0), rt.Point3(0, 100, 0), rt.Point3(0, 0, 100), rt.Point3(0, 0, 0))
                                
                                meshList.append(newMesh)
                                found = True
                
                if found:
                    continue
                
                vertList = []
                normList = []
                uvList = []
                faceList = []
                matList = []
                weightList = []
                indexList = []
                
                # fill faces first so we have the range of vert indices to use
                if mrge:
                    for sub in pInfo[5]:
                        for _ in range(sub[2]):
                            matList.append(sub[0])
                        rt.AppendIfUnique(usedShaders, sub[0])
                    faceList = pInfo[4]
                else:
                    for i in range(sInfo[2]):
                        matList.append(sInfo[0])
                        faceList.append(pInfo[4][sInfo[1] + i])
                    rt.AppendIfUnique(usedShaders, sInfo[0])
                
                if mrge:
                    for v in pInfo[3]:
                        vertList.append(v[0])
                        normList.append(v[1])
                        uvList.append(v[2])
                        if nIndex == 255:
                            indexList.append(v[3])
                            weightList.append(v[4])
                        else:
                            indexList.append(rt.Array(nIndex))
                            weightList.append(rt.Array(1))
                else:
                    # fill the vertList only with verts in the range of the face indices, and shift the indices to start at 1
                    vMin = pInfo[3].count
                    vMax = 1
                    for i in range(faceList.count):
                        if faceList[i].x < vMin:
                            vMin = faceList[i].x
                        if faceList[i].y < vMin:
                            vMin = faceList[i].y
                        if faceList[i].z < vMin:
                            vMin = faceList[i].z
                        if faceList[i].x > vMax:
                            vMax = faceList[i].x
                        if faceList[i].y > vMax:
                            vMax = faceList[i].y
                        if faceList[i].z > vMax:
                            vMax = faceList[i].z
                    for i in range(vMin, vMax):
                        vertList.append(pInfo[3][i][0])
                        normList.append(pInfo[3][i][1])
                        uvList.append(pInfo[3][i][2])
                        if nIndex == 255:
                            indexList.append(pInfo[3][i][3])
                            weightList.append(pInfo[3][i][4])
                        else:
                            indexList.append(rt.Array(nIndex))
                            weightList.append(rt.Array(1))
                    for i in range(faceList.count):
                        faceList[i] -= vMin - 1
                
                if uv:
                    newMesh = rt.Mesh(vertices=vertList, tverts=uvList, faces=faceList, materialids=matList)
                    rt.BuildTVFaces(newMesh)
                    for i in range(int(1), 1 + int(newMesh.numfaces)):
                        rt.SetTVFace(newMesh, i, rt.GetFace(newMesh, i))
                else:
                    newMesh = rt.Mesh(vertices=vertList, faces=faceList)
                
                if mrge:
                    newMesh.name = rName + ":" + pName
                else:
                    newMesh.name = rName + ":" + pName + ":" + str(s)
                
                # face group for CE region exports
                newMesh.faces[rName] = newMesh.faces
                
                if norm:
                    mxsshim.max("modify mode")
                    normMod = rt.Edit_Normals()
                    rt.Select(newMesh)
                    rt.AddModifier(newMesh, normMod)
                    normMod.selectby = 1
                    normMod.sellevel = rt.Name("Vertex")
                    normMod.Unify(selection=rt.bitarray(*(list(range(1, normMod.getnumnormals() + 1)))))
                    normMod.MakeExplicit(selection=rt.bitarray(*(list(range(1, normMod.getnumnormals() + 1)))))
                    for i in range(int(1), 1 + int(newMesh.numverts)):
                        sel = rt.BitArray(*([i]))
                        norms = rt.bitarray(*())
                        (_, sel, norms) = normMod.ConvertVertexSelection(pymxs.byref(sel), pymxs.byref(norms))
                        
                        for j in norms:
                            normMod.setnormal(j, normList[i - 1])
                    
                    # dont want annoying modifier with lines everywhere
                    rt.CollapseStack(newMesh)
                
                if version >= 0.1:
                    if not math.isnan(pInfo[8]):
                        mult = pInfo[8]
                        newMesh.transform = rt.Matrix3(rt.Point3(mult, 0, 0), rt.Point3(0, mult, 0), rt.Point3(0, 0, mult), rt.Point3(0, 0, 0))
                        newMesh.transform *= pInfo[9]
                        newMesh.transform *= rt.Matrix3(rt.Point3(100, 0, 0), rt.Point3(0, 100, 0), rt.Point3(0, 0, 100), rt.Point3(0, 0, 0))
                
                if unwrap:
                    uvMod = rt.Unwrap_UVW()
                    rt.AddModifier(newMesh, uvMod)
                
                if (vFormat > 0 or nIndex != 255) and weight:
                    mxsshim.max("modify mode")
                    theSkin = rt.Skin()
                    rt.Select(newMesh)
                    rt.AddModifier(newMesh, theSkin)
                    for b in boneList:
                        rt.skinOps.AddBone(theSkin, b, 0)
                    rt.RedrawViews()
                    for v in range(vertList.count):
                        rt.skinOps.ReplaceVertexWeights(theSkin, v + 1, indexList[v], weightList[v])
                    
                    if vFormat == 2 or nIndex != 255:
                        theSkin.rigid_vertices = True
                
                meshList.append(newMesh)
                meshDic.append(rt.Array(pInfo[6], newMesh))
    
    rt.CompleteRedraw()