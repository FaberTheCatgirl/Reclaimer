import unittest
import sys

from PySide2 import QtCore
from PySide2 import QtGui
from PySide2 import QtWidgets
from PySide2.QtWidgets import QApplication, QWidget, QMainWindow

from ..src.SceneReader import SceneReader
from ..UI.RmfWidget import *

class TestWindow(QMainWindow):
    def __init__(self):
        super(TestWindow, self).__init__(parent=None)
        self.setWindowTitle('Pyside Qt Window')
        # self.setWindowFlags(QtCore.Qt.Tool)
        scene = SceneReader.open_scene('C:\\Users\\Rhys\\Desktop\\100_citadel.rmf')
        
        # layout = QtWidgets.QVBoxLayout(self)
        content = RmfWidget(self, scene)
        self.setCentralWidget(content)
        # layout.addWidget(content, 1)
        # self.setLayout(layout)
        # content.adjustSize()
        # self.adjustSize()
        # self.setAttribute()
        self.resize(600, 600)

class PyMaxDockWidget(QWidget):
    def __init__(self, parent=None):
        super(PyMaxDockWidget, self).__init__(parent)
        # self.setWindowFlags(QtCore.Qt.Tool)
        # self.setWindowTitle('Pyside Qt  Dock Window')
        self.initUI()
        # self.setAttribute(QtCore.Qt.WA_QuitOnClose)

    def initUI(self):
        main_layout = QtWidgets.QVBoxLayout()
        label = QtWidgets.QLabel("Click button to create a cylinder in the scene")
        main_layout.addWidget(label)

        cylinder_btn = QtWidgets.QPushButton("Cylinder")
        #cylinder_btn.clicked.connect(make_cylinder)
        main_layout.addWidget(cylinder_btn)
        #widget = QtWidgets.QWidget()
        self.setLayout(main_layout)
        #self.setWidget(widget)
        self.resize(250, 100)

class Test_PySide(unittest.TestCase):
    def test_pyside(self):
        app = QApplication(sys.argv)
        w = TestWindow()
        w.show()
        app.exec_()

if __name__ == '__main__':
    unittest.main()
