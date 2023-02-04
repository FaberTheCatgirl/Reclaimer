import unittest
from ..src.SceneReader import SceneReader

class Test_Citadel(unittest.TestCase):
    def test_citadel(self):
        sr = SceneReader()
        sr.read_scene('C:\\Users\\Rhys\\Desktop\\100_citadel.rmf')

class Test_Brute(unittest.TestCase):
    def test_brute(self):
        sr = SceneReader()
        sr.read_scene('C:\\Users\\Rhys\\Desktop\\brute.rmf')

class Test_Masterchief(unittest.TestCase):
    def test_masterchief(self):
        sr = SceneReader()
        sr.read_scene('C:\\Users\\Rhys\\Desktop\\masterchief.rmf')

if __name__ == '__main__':
    unittest.main()
