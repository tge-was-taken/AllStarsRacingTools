using System;

namespace AllStarsRacingLib
{
    public class FileHashNotFoundException : Exception
    {
        public FileHashNotFoundException( string message ) : base( message )
        {

        }
    }
}
