var brnkly = {
    stores: ko.observable([]),
    activeStoreName: ko.observable(''),
    newInstance: {
        protocol: ko.observable('http'),
        host: ko.observable(''),
        port: ko.observable('8081'),
        virtualDirectory: ko.observable('')
    },
    canSave: false,
    canPublish: true,

    message: function (severity, text, nofade) {
        var msg = $('#message');

        msg.removeClass()
            .empty().append(text)
            .show()
            .addClass('alert')
            .addClass('alert-' + severity);

        if (!nofade) {
            msg.fadeTo(2000, 1).fadeOut();
        }
    },
    
    addStore: function () {
        var storeName = $('#store-name').val();
        brnkly.stores.push({
            name: ko.observable(storeName),
            instances: ko.observableArray()
        });
        brnkly.sortBy(brnkly.stores, 'name');
        $('#' + storeName  + '-content').addClass('in');
    },

    removeStore: function (store) {
        var index = brnkly.stores.indexOf(store);
        if (index >= 0) {
            brnkly.stores.splice(index, 1);
        }
    },

    setActiveStoreName: function (store) {
        brnkly.activeStoreName(store.name());
    },

    addInstance: function () {
        var activeStore;
        for (i = 0; i < brnkly.stores().length; i++) {
            if (brnkly.stores()[i].name() == brnkly.activeStoreName()) {
                activeStore = brnkly.stores()[i];
            }
        }

        if (!activeStore) { return; }

        var authority = brnkly.newInstance.host() + ':' + brnkly.newInstance.port();
        var instanceUri = brnkly.newInstance.protocol() + '://' + authority + '/' +
                          brnkly.newInstance.virtualDirectory() + '/databases/' +
                          activeStore.name().toLowerCase();

        // Add new instance as destination for all existing instances.
        for (i = 0; i < activeStore.instances().length; i++) {
            activeStore.instances()[i].destinations.push({
                uri: ko.observable(instanceUri),
                displayName: ko.observable(authority),
                replicate: ko.observable(false),
                isTransitive: ko.observable(false)
            });
            brnkly.sortBy(activeStore.instances()[i].destinations, 'displayName');
        }

        // Create new instance.
        var instanceObj = {
            uri: ko.observable(instanceUri),
            displayName: ko.observable(authority),
            allowReads: ko.observable(false),
            allowWrites: ko.observable(false),
            destinations: ko.observableArray()
        };
        activeStore.instances.push(instanceObj);
        brnkly.sortBy(activeStore.instances, 'displayName');

        // Add all instances as destinations for the new instance.
        for (i = 0; i < activeStore.instances().length; i++) {
            instanceObj.destinations.push({
                uri: ko.observable(activeStore.instances()[i].uri()),
                displayName: ko.observable(activeStore.instances()[i].displayName()),
                replicate: ko.observable(false),
                isTransitive: ko.observable(false)
            });
        }
    },

    save: function () {
        $.ajax({
            url: '/api/stores',
            type: 'POST',
            data: ko.mapping.toJSON(brnkly.stores),
            contentType: "application/json;charset=utf-8",
            success: function (data) {
                brnkly.message('success', 'Save succeeded');
            },
            error: function () {
                brnkly.message('important', 'Save failed', true);
            }
        });
    },

    sortBy: function (array, sortProperty) {
        array.sort(function (a, b) {
            return a[sortProperty]().toLowerCase() > b[sortProperty]().toLowerCase();
        });
    }
};

$.ready(function () {
    $('#working')
        .hide()
        .ajaxStart(function () {
            $(this).show();
        })
        .ajaxStop(function () {
            $(this).hide();
        });

    $.get("/api/stores", function (data) {
        brnkly.stores = ko.mapping.fromJS(data);;
        ko.applyBindings(brnkly.stores);
    });
}());