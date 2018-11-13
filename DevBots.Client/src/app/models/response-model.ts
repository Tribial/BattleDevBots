export class ResponseModel<T> {
    public model: T;
    public errors: string[] = [];
    public errorOccurred: boolean = false;
}
