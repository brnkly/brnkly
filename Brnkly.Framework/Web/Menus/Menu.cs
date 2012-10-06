using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using MvcContrib.PortableAreas;

namespace Brnkly.Framework.Web.Menus
{
    public class Menu
    {
        private static readonly ConcurrentDictionary<string, Menu> AllMenus =
            new ConcurrentDictionary<string, Menu>();
        private ConcurrentBag<MenuItem> items = new ConcurrentBag<MenuItem>();

        public string Name { get; set; }
        public IEnumerable<MenuItem> Items
        {
            get
            {
                return this.items
                    .OrderBy(item => item.Position)
                    .ThenBy(item => item.LinkText);
            }
        }

        public static Menu GetMenu(string name)
        {
            Menu menu;
            AllMenus.TryGetValue(name, out menu);
            return menu;
        }

        public class MenuItemAdder : IMessageHandler<AddMenuItem>
        {
            public void Handle(AddMenuItem message)
            {
                Menu menu;
                if (!AllMenus.TryGetValue(message.MenuItem.MenuName, out menu))
                {
                    AllMenus.TryAdd(message.MenuItem.MenuName, new Menu());
                }

                menu = AllMenus[message.MenuItem.MenuName];
                menu.items.Add(message.MenuItem);
            }

            public bool CanHandle(Type type)
            {
                return type == typeof(AddMenuItem);
            }

            public void Handle(object message)
            {
                this.Handle((AddMenuItem)message);
            }
        }
    }
}