//  MapAround - .NET tools for developing web and desktop mapping applications 

//  Copyright (coffee) 2009-2012 OOO "GKR"
//  This program is free software; you can redistribute it and/or 
//  modify it under the terms of the GNU General Public License 
//   as published by the Free Software Foundation; either version 3 
//  of the License, or (at your option) any later version. 
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program; If not, see <http://www.gnu.org/licenses/>



namespace MapAround.Web.Wms
{
    /// <summary>
    /// Stores contact metadata about WMS service.
    /// </summary>
    public struct WmsContactInformation
    {
        /// <summary>
        /// Address.
        /// </summary>
        public ContactAddress Address;

        /// <summary>
        /// E-mail address.
        /// </summary>
        public string ElectronicMailAddress;

        /// <summary>
        /// Fax number.
        /// </summary>
        public string FacsimileTelephone;

        /// <summary>
        /// Primary contact person.
        /// </summary>
        public ContactPerson PersonPrimary;

        /// <summary>
        /// Position of contact person.
        /// </summary>
        public string Position;

        /// <summary>
        /// Telephone.
        /// </summary>
        public string VoiceTelephone;

        #region ContactAddress

        /// <summary>
        /// Information about a contact address for the service.
        /// </summary>
        public struct ContactAddress
        {
            /// <summary>
            /// Contact address.
            /// </summary>
            public string Address;

            /// <summary>
            /// Type of address (usually "postal").
            /// </summary>
            public string AddressType;

            /// <summary>
            /// Contact City.
            /// </summary>
            public string City;

            /// <summary>
            /// Country of contact address.
            /// </summary>
            public string Country;

            /// <summary>
            /// Zipcode of contact.
            /// </summary>
            public string PostCode;

            /// <summary>
            /// State or province of contact.
            /// </summary>
            public string StateOrProvince;
        }

        #endregion

        #region ContactPerson

        /// <summary>
        /// Information about a contact person for the service.
        /// </summary>
        public struct ContactPerson
        {
            /// <summary>
            /// Organisation of primary person.
            /// </summary>
            public string Organisation;

            /// <summary>
            /// Primary contact person.
            /// </summary>
            public string Person;
        }

        #endregion
    }
}