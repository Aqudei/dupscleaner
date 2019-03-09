import os
import tempfile
import random

START_DIR = 'C:\\Users\\Dell\\Downloads\\Test Data'

if __name__ == '__main__':
    for root, dirs, files in os.walk(START_DIR):
        for i in range(random.randint(1, 100)):
            filename = 'testdoc{:02d}_V{:02d}.docx'.format(
                random.randint(1, 10), random.randint(1, 10))
            with open(os.path.join(root, filename), 'wt') as fp:
                fp.write('x')
