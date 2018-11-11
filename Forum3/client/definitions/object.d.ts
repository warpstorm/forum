declare interface ObjectConstructor {
	assign(...objects: Object[]): Object; // Necessary for IDEs with intellisense compilers targeting ES5.
}