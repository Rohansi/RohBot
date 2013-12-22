module.exports = function(grunt) {
	grunt.initConfig({
		pkg: grunt.file.readJSON('package.json'),
		sass: {
			dist: {
				files: {
					'dist/style.css': 'css/rohpod.scss'
				}
			}
		},
		copy: {
			main: {
				files: [
					{expand: true, src:['index.htm'], dest: 'dist/'},
					{expand: true, src:['img/*'], dest: 'dist/', flatten: true},
					{expand: true, src:['jslib/*.min.js'], dest: 'dist/', filter: 'isFile', flatten: true},
					{expand: true, src:['js/*.js'], dest: 'dist/', flatten: true}
				]
			}
		}
	});
	grunt.loadNpmTasks('grunt-contrib-sass');
	grunt.loadNpmTasks('grunt-contrib-copy');
	grunt.registerTask('default', ['copy', 'sass']);
};