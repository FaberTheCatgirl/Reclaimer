from typing import Tuple

__all__ = [
    'Color',
    'Float2',
    'Float3',
    'Float4',
    'Matrix3x3',
    'Matrix3x4',
    'Matrix4x4',
    'INamed'
]


Triangle = Tuple[int, int, int]
Color = Tuple[int, int, int, int]
Float2 = Tuple[float, float]
Float3 = Tuple[float, float, float]
Float4 = Tuple[float, float, float, float]
Matrix3x3 = Tuple[Float3, Float3, Float3]
Matrix3x4 = Tuple[Float3, Float3, Float3, Float3]
Matrix4x4 = Tuple[Float4, Float4, Float4, Float4]


class INamed:
    name: str = None

    def __str__(self) -> str:
        return self.name

    def __repr__(self) -> str:
        return f'<{str(self)}>'