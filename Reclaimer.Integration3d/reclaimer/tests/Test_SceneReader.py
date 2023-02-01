import unittest
from ..src.SceneReader import SceneReader

class Test_SceneReader(unittest.TestCase):
    def test_test(self):
        sr = SceneReader()
        sr.read_scene('C:\\Users\\Rhys\\Desktop\\100_citadel.rmf')

if __name__ == '__main__':
    unittest.main()
