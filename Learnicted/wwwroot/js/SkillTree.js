document.querySelectorAll('.node-circle').forEach(node => {
    node.addEventListener('click', function () {
        if (this.classList.contains('locked')) {
            alert("Hop! Önce bir önceki seviyeyi bitirip XP toplaman lazım. 😉");
            return;
        }

        const courseName = this.nextElementSibling.innerText;
        openCourseDetail(courseName);
    });
});

function openCourseDetail(name) {
    // Burada SweetAlert2 gibi bir kütüphane kullanabiliriz
    console.log(name + " ders detayları yükleniyor...");
}