using System;
using System.ComponentModel;

namespace Bodardr.Databinding.Runtime
{
    public class TestDataClass : INotifyPropertyChanged
    {
        public int MyInt { get; set; }

        public DateTime MyTime { get; set; }

        public string MyString { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}