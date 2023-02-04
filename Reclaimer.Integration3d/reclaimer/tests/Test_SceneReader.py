import unittest
from ..src.SceneReader import SceneReader

class Test_Citadel(unittest.TestCase):
    def test_citadel(self):
        scene = SceneReader.open_scene('C:\\Users\\Rhys\\Desktop\\100_citadel.rmf')
        return

class Test_Brute(unittest.TestCase):
    def test_brute(self):
        scene = SceneReader.open_scene('C:\\Users\\Rhys\\Desktop\\brute.rmf')
        return

class Test_Masterchief(unittest.TestCase):
    def test_masterchief(self):
        scene = SceneReader.open_scene('C:\\Users\\Rhys\\Desktop\\masterchief.rmf')
        return

if __name__ == '__main__':
    unittest.main()
