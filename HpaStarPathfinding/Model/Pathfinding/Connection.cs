using System.Collections.Generic;

namespace HpaStarPathfinding.ViewModel
{
    public class Connection
    {
        private Portal connectedPortal;//HashValue With 1 Bit Direction, 4 Bits Length, 12 bits posX and 12 bits posY
        private float cost;
    }
}