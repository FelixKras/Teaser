using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace TeaserDSV
{
    public class BitmapPool
    {
        private readonly ConcurrentDictionary<int, Bitmap> _objects;
        private List<int> _rentedBitmaps;
        private List<int> _freeBitmaps;
        private int _width,_height;
        public BitmapPool(int Width, int Height, int MaxObjCount)
        {
            _objects = new ConcurrentDictionary<int, Bitmap>();
            _rentedBitmaps = new List<int>();
            _freeBitmaps = new List<int>();
            _height=Height;
            _width = Width;

            for (int ii = 0; ii < MaxObjCount; ii++)
            {
                _objects.TryAdd(ii, new Bitmap(_width, _height, PixelFormat.Format32bppRgb));
                _freeBitmaps.Add(ii);
            }
        }

        public Bitmap GetObject()
        {
            Bitmap item;
            if (_freeBitmaps.Count > 0)
            {
                if (_objects.TryGetValue(_freeBitmaps[0], out item))
                {
                    _rentedBitmaps.Add(_freeBitmaps[0]);
                    _freeBitmaps.RemoveAt(0);
                }
                else
                {
                    item = new Bitmap(_width, _height, PixelFormat.Format32bppRgb);

                    //create more or reclaim _rented
                }
            }
            else
            {
                List<int> temps;
                temps = _freeBitmaps;
                _freeBitmaps = _rentedBitmaps;
                _rentedBitmaps = temps;
                item = new Bitmap(_width, _height, PixelFormat.Format32bppRgb);

            }
            return item;
        }

       
    }
}