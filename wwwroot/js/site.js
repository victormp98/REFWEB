// Cart Management
const Toast = Swal.mixin({
    toast: true,
    position: 'top-end',
    showConfirmButton: false,
    timer: 2000,
    timerProgressBar: true,
    didOpen: (toast) => {
        toast.addEventListener('mouseenter', Swal.stopTimer)
        toast.addEventListener('mouseleave', Swal.resumeTimer)
    }
});

const CartManager = {
    get: function () {
        return JSON.parse(localStorage.getItem('cart') || '[]');
    },
    add: function (id, name, price, img) {
        let cart = this.get();
        let item = cart.find(i => i.productoId === id);
        if (item) {
            item.cantidad++;
        } else {
            cart.push({ productoId: id, nombre: name, precio: price, imagenUrl: img, cantidad: 1 });
        }
        localStorage.setItem('cart', JSON.stringify(cart));
        this.updateBadge();
        window.dispatchEvent(new Event('cartUpdated'));

        Toast.fire({
            icon: 'success',
            title: `¡${name} añadido!`
        });
    },
    remove: function (id) {
        let cart = this.get();
        cart = cart.filter(i => i.productoId !== id);
        localStorage.setItem('cart', JSON.stringify(cart));
        this.updateBadge();
        window.dispatchEvent(new Event('cartUpdated'));

        Toast.fire({
            icon: 'info',
            title: 'Producto eliminado'
        });
    },
    updateQuantity: function (id, qty) {
        let cart = this.get();
        let item = cart.find(i => i.productoId === id);
        if (item) {
            item.cantidad = Math.max(1, qty);
            localStorage.setItem('cart', JSON.stringify(cart));
            this.updateBadge();
            window.dispatchEvent(new Event('cartUpdated'));
        }
    },
    clear: function () {
        localStorage.removeItem('cart');
        this.updateBadge();
        window.dispatchEvent(new Event('cartUpdated'));
    },
    updateBadge: function () {
        const cart = this.get();
        const totalItems = cart.reduce((sum, item) => sum + item.cantidad, 0);
        const badge = $('#cart-badge');
        badge.text(totalItems);
        badge.toggleClass('d-none', totalItems === 0);
    }
};

$(document).ready(function () {
    CartManager.updateBadge();
});
