from typing import Any, Union
from typing import cast

from PySide2 import QtCore
from PySide2 import QtWidgets
from PySide2.QtWidgets import QWidget, QTreeWidget, QTreeWidgetItem

from ..src import Scene, Model

CheckState = QtCore.Qt.CheckState
SceneItem = Union[Scene.SceneGroup, Model.Model, Model.ModelRegion, Model.ModelPermutation]

_typeDisplayNames = {
    Scene.SceneGroup: 'Group',
    Model.Model: 'Model',
    Model.ModelRegion: 'Region',
    Model.ModelPermutation: 'Permutation'
}

class RmfTreeItem(QTreeWidgetItem):
    def __init__(self, parent: Union[QTreeWidget, 'RmfTreeItem'], item: SceneItem):
        super(RmfTreeItem, self).__init__(parent)
        self.setCheckState(0, CheckState.Checked)
        self.setText(0, str(item))
        self.setText(1, _typeDisplayNames[type(item)])

    def setData(self, column: int, role: int, value: Any):
        super(RmfTreeItem, self).setData(column, role, value)
        if column == 0 and role == QtCore.Qt.CheckStateRole:
            self._refresh()

    def _refresh(self):
        hideChildren = self.checkState(0) == CheckState.Unchecked
        for i in range(self.childCount()):
            c = cast(RmfTreeItem, self.child(i))
            c.setHidden(hideChildren)
            if not hideChildren:
                c._refresh()


class RmfWidget(QWidget):
    _scene: Scene.Scene
    _treeview: QTreeWidget

    def __init__(self, parent: QWidget, scene: Scene.Scene):
        super(RmfWidget, self).__init__(parent)
        self._scene = scene

        treePanel = QtWidgets.QVBoxLayout()
        toolbar = self._create_toolbar()
        treeView = self._create_treeview()
        
        layout = QtWidgets.QGridLayout()
        layout.addWidget(treeView)

        treePanel.addWidget(toolbar)
        treePanel.addLayout(layout, 1)

        footer = self._create_footer()
        treePanel.addLayout(footer, 0)

        self.setLayout(treePanel)

    def _create_toolbar(self):
        toolbar = QtWidgets.QToolBar()

        expNone = QtWidgets.QPushButton('[-]')
        expNone.clicked.connect(lambda: self._treeview.collapseAll())
        toolbar.addWidget(expNone)

        expAll = QtWidgets.QPushButton('[+]')
        expAll.clicked.connect(lambda: self._treeview.expandAll())
        toolbar.addWidget(expAll)

        return toolbar

    def _create_footer(self):
        panel = QtWidgets.QVBoxLayout()
        button = QtWidgets.QPushButton('Import')
        panel.addWidget(button, 0, QtCore.Qt.AlignHCenter)
        return panel

    def _create_treeview(self):
        treeView = self._treeview = QTreeWidget()

        treeView.setColumnCount(2)
        treeView.setAlternatingRowColors(True)
        treeView.setHeaderLabels(['Name', 'Type'])

        treeView.addTopLevelItems([self._build_group(treeView, g) for g in self._scene.root_node.child_groups])
        treeView.expandAll()
        treeView.resizeColumnToContents(0)
        treeView.resizeColumnToContents(1)
        treeView.adjustSize()
        
        return treeView

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

