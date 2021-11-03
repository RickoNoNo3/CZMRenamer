using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows.Media;

namespace CZMRenamer {
    public class ListItem : INotifyPropertyChanged {
        public event PropertyChangedEventHandler PropertyChanged;

        public string Name { get; set; }
        private bool _exists;
        public bool Exists {
            get {
                return _exists;
            }
            set {
                _exists = value;
                // Exists被改变，触发的属性改变事件实际是ShownColor
                this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("ShownColor"));
            }
        }
        public Brush ShownColor {
            get {
                return Exists ? Brushes.LightGreen : Brushes.White;
            }
        }

        public override bool Equals(object o) {
            if (o.GetType() != typeof(ListItem)) {
                return false;
            }
            if ((o as ListItem).Name != this.Name) {
                return false;
            }
            return true;
        }

        public override int GetHashCode() {
            return base.GetHashCode();
        }
    };
}
