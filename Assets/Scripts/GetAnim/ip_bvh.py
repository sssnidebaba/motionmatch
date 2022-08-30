import bpy
import os
import sys

objs = bpy.data.objects
ipath = sys.argv[-3]
opath = sys.argv[-2]
batchSize = int(sys.argv[-1])

filters = ["t"] # 过滤的fbx文件

def import_bvh(ipath, filters, opath, size):
    name = []
    file_lst = os.listdir(ipath)
    objs = bpy.data.objects
    n = 0

    for item in file_lst:
        fileName, fileExtension = os.path.splitext(item)
        if fileExtension == ".bvh" and (not fileName in filters):
            #导入bvh文件 假如达到batchSize 导出fbx 然后重置场景
            bpy.ops.import_anim.bvh(filepath=(ipath + "\\" + item), axis_up='Y', axis_forward='-Z', filter_glob="*.bvh", target='ARMATURE', global_scale=1.0, frame_start=1, use_fps_scale=False, use_cyclic=False, rotate_mode='NATIVE')
            name.append(fileName)
            n += 1
            if n >= size:
                for i in range(size-1):
                    objs.remove(objs[name[i]], do_unlink=True)
                bpy.ops.export_scene.fbx(filepath=opath+name[size-1]+".fbx")
                objs.remove(objs[name[size-1]], do_unlink=True)
                for anim in bpy.data.actions:
                    bpy.data.actions.remove(anim)
                name.clear()
                n = 0
    nLen = len(name)
    #未达到batchSize部分的处理
    if nLen == 1:
        bpy.ops.export_scene.fbx(filepath=opath+name[nLen-1]+".fbx")
        return
    if nLen > 0:
        for i in range(nLen-1):
            objs.remove(objs[name[i]], do_unlink=True)
        bpy.ops.export_scene.fbx(filepath=opath+name[nLen-1]+".fbx")
    return

#实际运行部分(main函数)
#删除blender默认cube light camera
for obj in objs:
    objs.remove(obj, do_unlink=True)
import_bvh(ipath, filters, opath, batchSize)
#删除场景所有物体与动画
for obj in objs:
    objs.remove(obj, do_unlink=True)
for anim in bpy.data.actions:
    bpy.data.actions.remove(anim)