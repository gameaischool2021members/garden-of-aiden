import sys
from argparse import *
from cDCGAN import Model

def main():
    parser = ArgumentParser(description='Load model from filesystem and use for inference')
    parser.add_argument('--model', help='The path to the .h5 stored model', required=True, type=str)
    args = parser.parse_args()

    model = Model.ModelRunner(args.model)
    model.listen()

if __name__ == '__main__':
    sys.exit(main())
