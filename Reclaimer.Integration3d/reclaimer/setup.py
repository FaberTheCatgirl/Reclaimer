import setuptools

# with open("README.md", "r") as fh:
#     long_description = fh.read()

setuptools.setup(
    name="reclaimer-rmf-importer",
    version="0.0.0",
    author="Gravemind2401",
    # author_email="some.dude@autodesk.com",
    description="Import RMF files",
    # long_description=long_description,
    # long_description_content_type="text/markdown",
    # url="https://git.autodesk.com/windish/maxpythontutorials",
    packages=setuptools.find_packages(),
    # classifiers=[
    #     "Programming Language :: Python :: 3",
    #     "LICENSE :: OTHER/PROPRIETARY LICENSE",
    #     "Operating System :: Microsoft :: Windows"
    # ],
    python_requires='>=3.7'
)