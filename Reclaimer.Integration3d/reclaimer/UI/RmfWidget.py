from typing import Union
from PySide2 import QtCore
from PySide2 import QtWidgets
from PySide2.QtWidgets import QWidget, QTreeWidget, QTreeWidgetItem

from ..src import Scene, Model

CheckState = QtCore.Qt.CheckState
SceneItem = Union[Scene.SceneGroup, Model.Model, Model.ModelRegion, Model.ModelPermutation]

class RmfTreeItem(QTreeWidgetItem):
    def __init__(self, parent: Union[QTreeWidget, 'RmfTreeItem'], item: SceneItem):
        super(RmfTreeItem, self).__init__(parent)
        self.setCheckState(0, CheckState.Checked)
        self.setText(0, str(item))
        self.setText(1, type(item).__name__)


class RmfWidget(QWidget):
    _scene: Scene.Scene

    def __init__(self, parent: QWidget, scene: Scene.Scene):
        super(RmfWidget, self).__init__(parent)
        self._scene = scene
        layout = QtWidgets.QGridLayout()
        
        treeView = QTreeWidget()
        treeView.setColumnCount(2)
        treeView.setAlternatingRowColors(True)
        treeView.setHeaderLabels(['Name', 'Type'])

        layout.addWidget(treeView)

        treeView.addTopLevelItems([self._build_group(treeView, g) for g in scene.root_node.child_groups])
        treeView.expandAll()
        treeView.resizeColumnToContents(0)
        treeView.resizeColumnToContents(1)
        treeView.adjustSize()

        self.setLayout(layout)

    def _build_group(self, parent: Union[QTreeWidget, 'RmfTreeItem'], group: Scene.SceneGroup) -> RmfTreeItem:
        item = RmfTreeItem(parent, group)
        item.addChildren([self._build_group(item, g) for g in group.child_groups])
        item.addChildren([self._build_object(item, obj) for obj in group.child_objects])
        return item

    def _build_object(self, parent: Union[QTreeWidget, 'RmfTreeItem'], object: SceneItem) -> RmfTreeItem:
        if type(object) == Scene.ModelRef:
            object = self._scene.model_pool[object.model_index]

        item = RmfTreeItem(parent, object)
        if type(object) == Model.Model:
            item.addChildren([self._build_object(item, r) for r in object.regions])
        elif type(object) == Model.ModelRegion:
            item.addChildren([self._build_object(item, p) for p in object.permutations])

        return item

