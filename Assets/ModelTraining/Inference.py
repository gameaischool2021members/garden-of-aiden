import sys
from argparse import *
from cDCGAN import Model

def main():
    parser = ArgumentParser(description='Load model from filesystem and use for inference')
    parser.add_argument('--model', help='The path to the .h5 stored model', required=True, type=str)
    parser.parse_args()

    print('hi first')
    # model = Model.ModelRunner(parser.model)
    print('hi')
    # model.listen()

if __name__ == '__main':
    sys.exit(main())
